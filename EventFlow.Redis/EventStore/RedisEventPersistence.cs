using EventFlow.Core;
using EventFlow.EventStores;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EventFlow.Redis.EventStore;

public class RedisEventPersistence : IEventPersistence
{
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly ILogger<RedisEventPersistence> _logger;
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

        var streamNames = await _resolver.GetStreamNamesAsync(cancellationToken);
        var streamTasks = streamNames.Select(prefixedKey => GetCommittedEventsAsync(prefixedKey, startPosition, cancellationToken, pageSize)).ToList();

        await Task.WhenAll(streamTasks);
        var events = streamTasks.SelectMany(t => t.Result);

        var nextPos = events.Any()
            ? events.Max(e => e.AggregateSequenceNumber)
            : startPosition;

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
            //Redis stream entry id uses the format: <UnixTime>-<IncrementingId>
            var messageId =
                new RedisValue(
                    $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{serializedEvent.AggregateSequenceNumber}");

            var data = new NameValueEntry(new RedisValue("data"), new RedisValue(serializedEvent.SerializedData));
            var metadata = new NameValueEntry(new RedisValue("metadata"),
                new RedisValue(serializedEvent.SerializedMetadata));

            var result = await database.StreamAddAsync(prefixedKey, new[] {data, metadata}, messageId);
            if (result == messageId)
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                    _logger.LogTrace("Committed event with id {EventId} to Redis for aggregate with Id {AggregateId}",
                        prefixedKey.Key, messageId);

                committedEvents.Add(new RedisCommittedDomainEvent(prefixedKey.Key, data.Value, metadata.Value,
                    serializedEvent.AggregateSequenceNumber));
            }
            else
            {
                _logger.LogWarning(
                    "Failed to commit event with id {EventId} to Redis for aggregate with Id {AggregateId}, {Result}",
                    prefixedKey.Key, messageId, result);
            }
        }

        return committedEvents;
    }

    public async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(IIdentity id,
        int fromEventSequenceNumber, CancellationToken cancellationToken)
    {
        var prefixedKey = GetAsPrefixedKey(id.Value);
        var events = await GetCommittedEventsAsync(prefixedKey, fromEventSequenceNumber, cancellationToken);

        return events.ToList();
    }

    public async Task DeleteEventsAsync(IIdentity id, CancellationToken cancellationToken)
    {
        var database = _multiplexer.GetDatabase();
        var keyWithPrefix = GetAsPrefixedKey(id.Value);

        var result = await database.KeyDeleteAsync(keyWithPrefix);
        if (!result)
            _logger.LogWarning("Failed to delete the Redis Stream with id {Id}", id.Value);
    }

    private async Task<IEnumerable<RedisCommittedDomainEvent>> GetCommittedEventsAsync(PrefixedKey prefixedKey,
        long fromPosition,
        CancellationToken token, int? limit = null)
    {
        var database = _multiplexer.GetDatabase();
        var result = await database.StreamReadAsync(prefixedKey, fromPosition, limit);
        if (!result.Any())
            return Array.Empty<RedisCommittedDomainEvent>();

        var committedEvents = result.Select(se =>
        {
            var data = se.Values.FirstOrDefault(v => v.Name == "data").Value;
            var metadata = se.Values.FirstOrDefault(v => v.Name == "metadata").Value;
            var sequenceNumber = int.Parse(((string) se.Id).Split("-").Last());

            return new RedisCommittedDomainEvent(prefixedKey.Key, data, metadata, sequenceNumber);
        });

        return committedEvents.ToList();
    }
    
    private static PrefixedKey GetAsPrefixedKey(string id) 
        => new PrefixedKey(Constants.StreamPrefix, id);
}