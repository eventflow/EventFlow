// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
// https://github.com/eventflow/EventFlow
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Exceptions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EventFlow.Redis.EventStore;

internal class RedisEventPersistence : IEventPersistence
{
    private readonly ILogger<RedisEventPersistence> _logger;
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly IEventStreamCollectionResolver _resolver;

    public RedisEventPersistence(IConnectionMultiplexer multiplexer, ILogger<RedisEventPersistence> logger,
        IEventStreamCollectionResolver resolver)
    {
        _multiplexer = multiplexer;
        _logger = logger;
        _resolver = resolver;
    }

    public async Task<AllCommittedEventsPage> LoadAllCommittedEvents(GlobalPosition globalPosition, int pageSize,
        CancellationToken cancellationToken)
    {
        var startPosition = globalPosition.IsStart
            ? 0
            : long.Parse(globalPosition.Value);

        var streamNames = await _resolver.GetStreamIdsAsync(cancellationToken).ConfigureAwait(false);
        var streamTasks = streamNames.Select(prefixedKey =>
            GetCommittedEventsAsync(prefixedKey, startPosition, cancellationToken, pageSize)).ToList();

        await Task.WhenAll(streamTasks).ConfigureAwait(false);
        var events = streamTasks.SelectMany(t => t.Result);

        var nextPos = events.Any()
            ? events.Max(e => e.AggregateSequenceNumber)
            : startPosition;

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Loaded {Count} events from redis", events.Count());
        }

        return new AllCommittedEventsPage(new GlobalPosition(nextPos.ToString()), events.ToList());
    }

    public async Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync(IIdentity id,
        IReadOnlyCollection<SerializedEvent> serializedEvents, CancellationToken cancellationToken)
    {
        var committedEvents = new List<RedisCommittedDomainEvent>();
        var database = _multiplexer.GetDatabase();
        var prefixedKey = GetAsPrefixedKey(id.Value);

        foreach (var serializedEvent in serializedEvents)
        {
            //Redis stream entry id uses the format: <UnixTime>-<IncrementingId>, we leave the timestamp blank to ensure optimistic concurrency works
            var messageId = $"0-{serializedEvent.AggregateSequenceNumber}";

            var data = new NameValueEntry("data", serializedEvent.SerializedData);
            var metadata = new NameValueEntry("metadata", serializedEvent.SerializedMetadata);

            try
            {
                await database
                    .StreamAddAsync(prefixedKey, new[] {data, metadata}, messageId)
                    .ConfigureAwait(false);

                if (_logger.IsEnabled(LogLevel.Trace))
                    _logger.LogTrace(
                        "Committed event with id {EventId} for aggregate with Id {AggregateId} to Redis ",
                        messageId, prefixedKey.Key);

                committedEvents.Add(new RedisCommittedDomainEvent(prefixedKey.Key, data.Value, metadata.Value,
                    serializedEvent.AggregateSequenceNumber));
            }
            catch (RedisServerException e)
            {
                if (e.Message.Contains(
                        "ERR The ID specified in XADD is equal or smaller than the target stream top item"))
                    throw new OptimisticConcurrencyException(messageId, e);
            }
        }

        return committedEvents;
    }

    public async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(IIdentity id,
        int fromEventSequenceNumber, CancellationToken cancellationToken)
    {
        var prefixedKey = GetAsPrefixedKey(id.Value);
        var events = await GetCommittedEventsAsync(prefixedKey, fromEventSequenceNumber, cancellationToken)
            .ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("Loaded {Count} events for aggregate with id {AggregateId} from redis", events.Count(),
                id.Value);

        return events.ToList();
    }

    public async Task DeleteEventsAsync(IIdentity id, CancellationToken cancellationToken)
    {
        var database = _multiplexer.GetDatabase();
        var keyWithPrefix = GetAsPrefixedKey(id.Value);

        var result = await database.KeyDeleteAsync(keyWithPrefix).ConfigureAwait(false);
        if (!result)
        {
            _logger.LogWarning("Failed to delete the Redis Stream with id {Id}", id.Value);
            return;
        }

        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("Deleted events for aggregate with id {AggregateId}", id.Value);
    }

    private async Task<IEnumerable<RedisCommittedDomainEvent>> GetCommittedEventsAsync(PrefixedKey prefixedKey,
        long fromPosition,
        CancellationToken token, int? limit = null)
    {
        //Stackexchange.Redis uses XREAD to read streams, which does not include the item at fromPosition, so we have to start at fromPosition -1
        var fromMessageId = fromPosition == 0 ? $"0-{fromPosition}" : $"0-{fromPosition - 1}";
        var database = _multiplexer.GetDatabase();

        var result = await database.StreamReadAsync(prefixedKey, fromMessageId, count: limit).ConfigureAwait(false);
        if (!result.Any())
        {
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace("No events found for aggregate {AggregateId}", prefixedKey.Key);
            return Array.Empty<RedisCommittedDomainEvent>();
        }

        var committedEvents = result.Select(se =>
        {
            var data = se.Values.FirstOrDefault(v => v.Name == "data").Value;
            var metadata = se.Values.FirstOrDefault(v => v.Name == "metadata").Value;
            var sequenceNumber = int.Parse(((string) se.Id).Split("-").Last());

            return new RedisCommittedDomainEvent(prefixedKey.Key, data, metadata, sequenceNumber);
        });

        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("Found {Count} events for aggregate with id {AggregateId}", committedEvents.Count(),
                prefixedKey.Key);

        return committedEvents.ToList();
    }

    private static PrefixedKey GetAsPrefixedKey(string id)
        => new PrefixedKey(Constants.StreamPrefix, id);
}