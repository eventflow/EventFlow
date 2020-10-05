// The MIT License (MIT)
// 
// Copyright (c) 2015-2020 Rasmus Mikkelsen
// Copyright (c) 2015-2020 eBay Software Foundation
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
using EventFlow.Logs;
using EventStore.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.EventStores.EventStore
{
    public class EventStoreEventPersistence : IEventPersistence
    {
        private readonly ILog _log;
        private readonly EventStoreClient _eventStoreConnection;

        public EventStoreEventPersistence(
            ILog log,
            EventStoreClient eventStoreConnection)
        {
            _log = log;
            _eventStoreConnection = eventStoreConnection;
        }

        public async Task<AllCommittedEventsPage> LoadAllCommittedEvents(
            GlobalPosition globalPosition,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var startPosition = ParsePosition(globalPosition);

            var response = await _eventStoreConnection.ReadAllAsync(
                    Direction.Forwards,
                    startPosition,
                    maxCount: pageSize
                )
                .ToListAsync();

            var eventStoreEvents = Map(response);

            Position nextPosition = eventStoreEvents.Any()
                ? response.Last().Event.Position
                : Position.Start;

            return new AllCommittedEventsPage(
                new GlobalPosition(string.Format("{0}-{1}", nextPosition.CommitPosition, nextPosition.PreparePosition)),
                eventStoreEvents);
        }

        public async Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync(
            IIdentity id,
            IReadOnlyCollection<SerializedEvent> serializedEvents,
            CancellationToken cancellationToken)
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

            ulong expectedVersion = (ulong)Math.Max((long)serializedEvents.Min(e => e.AggregateSequenceNumber) - 2, StreamState.NoStream.ToInt64());

            var eventDatas = serializedEvents
                .Select(e =>
                {
                    var eventType = string.Format("{0}.{1}.{2}", e.Metadata[MetadataKeys.AggregateName], e.Metadata.EventName, e.Metadata.EventVersion);
                    var data = Encoding.UTF8.GetBytes(e.SerializedData);
                    var meta = Encoding.UTF8.GetBytes(e.SerializedMetadata);
                    return new EventData(Uuid.NewUuid(), eventType, data, meta);
                })
                .ToList();

            try
            {
                var writeResult = await _eventStoreConnection.AppendToStreamAsync(
                    id.Value,
                    new StreamRevision(expectedVersion),
                    eventDatas)
                    .ConfigureAwait(false);

                _log.Verbose(
                    "Wrote entity {0} with version {1} ({2},{3})",
                    id,
                    writeResult.NextExpectedStreamRevision,
                    writeResult.LogPosition.CommitPosition,
                    writeResult.LogPosition.PreparePosition);
            }
            catch (WrongExpectedVersionException e)
            {
                throw new OptimisticConcurrencyException(e.Message, e);
            }

            return committedDomainEvents;
        }

        public async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(
            IIdentity id,
            int fromEventSequenceNumber,
            CancellationToken cancellationToken)
        {
            EventStoreClient.ReadStreamResult response = _eventStoreConnection.ReadStreamAsync(
                Direction.Forwards,
                id.Value,
                fromEventSequenceNumber <= 1
                    ? StreamPosition.Start
                    : StreamPosition.FromInt64(fromEventSequenceNumber) - 1 // EventStore uses a "zero-based" index
            );

            if (await response.ReadState == ReadState.StreamNotFound)
                return new List<ICommittedDomainEvent>();

            return Map(await response.ToListAsync());
        }

        public async Task DeleteEventsAsync(IIdentity id, CancellationToken cancellationToken)
        {
            await _eventStoreConnection.SoftDeleteAsync(id.Value, StreamRevision.None, cancellationToken: cancellationToken);

            return;
        }

        private IReadOnlyCollection<EventStoreEvent> Map(IEnumerable<ResolvedEvent> resolvedEvents)
        {
            return resolvedEvents
                .Select(e => new EventStoreEvent
                {
                    AggregateId = e.OriginalStreamId,
                    AggregateSequenceNumber = (int)e.Event.EventNumber.ToInt64(),
                    Metadata = Encoding.UTF8.GetString(e.Event.Metadata.Span),
                    Data = Encoding.UTF8.GetString(e.Event.Data.Span),
                })
                .ToList();
        }

        private Position ParsePosition(GlobalPosition globalPosition)
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
    }
}