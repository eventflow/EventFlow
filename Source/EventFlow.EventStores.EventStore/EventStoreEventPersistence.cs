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

using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Exceptions;
using EventStore.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.EventStores.EventStore;

public class EventStoreEventPersistence(EventStoreClient eventStoreClient, ILogger<EventStoreEventPersistence> logger) : IEventPersistence
{
    private class EventStoreEvent : ICommittedDomainEvent
    {
        public required string AggregateId { get; set; }
        public required string Data { get; set; }
        public required string Metadata { get; set; }
        public required int AggregateSequenceNumber { get; set; }
    }

    public async Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync(IIdentity id, IReadOnlyCollection<SerializedEvent> serializedEvents, CancellationToken cancellationToken)
    {
        var committedDomainEvents = serializedEvents
            .Select(e => new EventStoreEvent
            {
                AggregateSequenceNumber = e.AggregateSequenceNumber,
                Metadata = e.SerializedMetadata,
                AggregateId = id.Value,
                Data = e.SerializedData
            })
            .ToList();

        //var expectedVersion = Math.Max(serializedEvents.Min(e => e.AggregateSequenceNumber) - 2, ExpectedVersion.NoStream);
        var expectedVersion = serializedEvents.Min(e => e.AggregateSequenceNumber) - 2;

        var eventDatas = serializedEvents
            .Select(e =>
                {
                    // While it might be tempting to use e.Metadata.EventId here, we can't
                    // as EventStore won't detect optimistic concurrency exceptions then
                    var uuid = Uuid.NewUuid();

                    var eventType = string.Format("{0}.{1}.{2}", e.Metadata[MetadataKeys.AggregateName], e.Metadata.EventName, e.Metadata.EventVersion);
                    var data = Encoding.UTF8.GetBytes(e.SerializedData);
                    var meta = Encoding.UTF8.GetBytes(e.SerializedMetadata);
                    return new EventData(uuid, eventType, data, meta);
                })
            .ToList();

        try
        {
            IWriteResult writeResult;
            if (expectedVersion < 0)
            {
                writeResult = await eventStoreClient.AppendToStreamAsync(
                    id.Value,
                    StreamState.NoStream,
                    eventDatas,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                writeResult = await eventStoreClient.AppendToStreamAsync(
                    id.Value,
                    StreamRevision.FromInt64(expectedVersion),
                    eventDatas,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            logger.LogDebug(
                "Wrote entity {0} with version {1} ({2},{3})",
                id,
                expectedVersion,
                writeResult.LogPosition.CommitPosition,
                writeResult.LogPosition.PreparePosition);
        }
        catch (WrongExpectedVersionException e)
        {
            throw new OptimisticConcurrencyException(e.Message, e);
        }

        return committedDomainEvents;
    }

    public async Task DeleteEventsAsync(IIdentity id, CancellationToken cancellationToken)
    {
        await eventStoreClient.TombstoneAsync(id.Value, StreamState.Any, cancellationToken: cancellationToken);
    }

    public async Task<AllCommittedEventsPage> LoadAllCommittedEvents(GlobalPosition globalPosition, int pageSize, CancellationToken cancellationToken)
    {
        var nextPosition = ParsePosition(globalPosition);
        var resolvedEvents = new List<ResolvedEvent>();

        await foreach (var resolvedEvent in eventStoreClient.ReadAllAsync(
            Direction.Forwards, nextPosition, pageSize, cancellationToken: cancellationToken))
        {
            if (!resolvedEvent.OriginalStreamId.StartsWith("$"))
            {
                resolvedEvents.Add(resolvedEvent);
            }
        }

        var eventStoreEvents = Map(resolvedEvents);

        return new AllCommittedEventsPage(
            new GlobalPosition($"{resolvedEvents.Last().Event.Position.CommitPosition}-{resolvedEvents.Last().Event.Position.PreparePosition}"),
            eventStoreEvents);
    }

    public async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(IIdentity id, int fromEventSequenceNumber, CancellationToken cancellationToken)
    {
        return await LoadCommittedEventsAsync(id, fromEventSequenceNumber, 0, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(IIdentity id, int fromEventSequenceNumber, int toEventSequenceNumber, CancellationToken cancellationToken)
    {
        var resolvedEvents = new List<ResolvedEvent>();

        var startPosition = fromEventSequenceNumber <= 1
            ? StreamPosition.Start
            : StreamPosition.FromInt64(fromEventSequenceNumber - 1);

        try
        {
            ReadState result = await eventStoreClient.ReadStreamAsync(
                Direction.Forwards,
                id.Value,
                startPosition,
                cancellationToken: cancellationToken
            ).ReadState;

            if (result == ReadState.StreamNotFound)
            {
                // No events, the stream doesn't exist i.e. IsNew
                return [];
            }

            await foreach (var resolvedEvent in eventStoreClient.ReadStreamAsync(
                Direction.Forwards,
                id.Value,
                startPosition,
                cancellationToken: cancellationToken).WithCancellation(cancellationToken))
            {
                resolvedEvents.Add(resolvedEvent);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to load committed events for {id.Value}", ex);
        }

        return Map(resolvedEvents);
    }

    private static Position ParsePosition(GlobalPosition globalPosition)
    {
        if (globalPosition.IsStart)
        {
            return Position.Start;
        }

        var parts = globalPosition.Value.Split('-');
        if (parts.Length != 2)
        {
            throw new ArgumentException(string.Format(
                "Unknown structure for global position '{0}'. Expected it to be empty or in the form 'L-L'",
                globalPosition.Value));
        }

        var commitPosition = ulong.Parse(parts[0]);
        var preparePosition = ulong.Parse(parts[1]);

        return new Position(commitPosition, preparePosition);
    }

    private static List<EventStoreEvent> Map(IEnumerable<ResolvedEvent> resolvedEvents)
    {
        return resolvedEvents
            .Select(e => new EventStoreEvent
            {
                AggregateSequenceNumber = (int)(e.Event.EventNumber.ToInt64() + 1), // Starts from zero
                Metadata = Encoding.UTF8.GetString(e.Event.Metadata.ToArray()),
                AggregateId = e.Event.EventStreamId,
                Data = Encoding.UTF8.GetString(e.Event.Data.ToArray()),
            })
            .ToList();
    }
}
