// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
// https://github.com/rasmus/EventFlow
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Logs;
using EventStore.ClientAPI;

namespace EventFlow.EventStores.EventStore
{
    public class EventStoreEventStore : EventStoreBase
    {
        private readonly IEventStoreConnection _connection;

        private class EventStoreEvent : ICommittedDomainEvent
        {
            public Guid BatchId { get; set; }
            public string AggregateId { get; set; }
            public string AggregateName { get; set; }
            public string Data { get; set; }
            public string Metadata { get; set; }
            public int AggregateSequenceNumber { get; set; }
        }

        public EventStoreEventStore(
            ILog log,
            IAggregateFactory aggregateFactory,
            IEventJsonSerializer eventJsonSerializer,
            IEventUpgradeManager eventUpgradeManager,
            IEventStoreConnection connection,
            IEnumerable<IMetadataProvider> metadataProviders)
            : base(log, aggregateFactory, eventJsonSerializer, eventUpgradeManager, metadataProviders)
        {
            _connection = connection;
        }

        protected override Task<AllCommittedEventsPage> LoadAllCommittedDomainEvents(long startPostion, long endPosition, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override async Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync<TAggregate, TIdentity>(
            TIdentity id,
            IReadOnlyCollection<SerializedEvent> serializedEvents,
            CancellationToken cancellationToken)
        {
            var batchId = Guid.NewGuid();
            var aggregateName = typeof (TAggregate).Name;
            var committedDomainEvents = serializedEvents
                .Select(e => new EventStoreEvent
                    {
                        AggregateSequenceNumber = e.AggregateSequenceNumber,
                        Metadata = e.SerializedMetadata,
                        AggregateId = id.Value,
                        AggregateName = aggregateName,
                        BatchId = batchId,
                        Data = e.SerializedData
                    })
                .ToList();

            var expectedVersion = Math.Max(serializedEvents.Min(e => e.AggregateSequenceNumber) - 1, 0);
            var eventDatas = serializedEvents
                .Select(e =>
                    {
                        var guid = Guid.Parse(e.Metadata["guid"]);
                        var eventType = string.Format("{0}.{1}", e.Metadata.EventName, e.Metadata.EventVersion);
                        var data = Encoding.UTF8.GetBytes(e.SerializedData);
                        var meta = Encoding.UTF8.GetBytes(e.SerializedData);
                        return new EventData(guid, eventType, true, data, meta);
                    });

            await _connection.AppendToStreamAsync(
                id.Value,
                expectedVersion,
                eventDatas)
                .ConfigureAwait(false);

            return committedDomainEvents;
        }

        protected override async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync<TAggregate, TIdentity>(
            TIdentity id,
            CancellationToken cancellationToken)
        {
            var streamEvents = new List<ResolvedEvent>();

            StreamEventsSlice currentSlice;
            var nextSliceStart = StreamPosition.Start;
            do
            {
                currentSlice = await _connection.ReadStreamEventsForwardAsync(
                    id.Value,
                    nextSliceStart,
                    200,
                    false)
                    .ConfigureAwait(false);
                nextSliceStart = currentSlice.NextEventNumber;
                streamEvents.AddRange(currentSlice.Events);

            } while (!currentSlice.IsEndOfStream);

            // TODO: Move to eventstore and into meta data
            var batchId = Guid.NewGuid();

            var eventStoreEvents = streamEvents
                .Select(e => new EventStoreEvent
                    {
                        AggregateSequenceNumber = e.Event.EventNumber,
                        Metadata = Encoding.UTF8.GetString(e.Event.Metadata),
                        AggregateId = id.Value,
                        AggregateName = typeof (TAggregate).Name,
                        Data = Encoding.UTF8.GetString(e.Event.Data),
                        BatchId = batchId,
                    })
                .ToList();
            return eventStoreEvents;
        }

        public override Task DeleteAggregateAsync<TAggregate, TIdentity>(
            TIdentity id,
            CancellationToken cancellationToken)
        {
            return _connection.DeleteStreamAsync(id.Value, ExpectedVersion.Any);
        }
    }
}
