// The MIT License (MIT)
//
// Copyright (c) 2015 EventFlow
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
using System.Threading.Tasks;
using Common.Logging;
using EventFlow.MsSql;

namespace EventFlow.EventStores.MsSql
{
    public class EventStore : IEventStore
    {
        public class EventDataModel : ICommittedDomainEvent
        {
            public long GlobalSequenceNumber { get; set; }
            public Guid BatchId { get; set; }
            public string AggregateId { get; set; }
            public string AggregateName { get; set; }
            public string Data { get; set; }
            public string Metadata { get; set; }
            public int AggregateSequenceNumber { get; set; }
        }

        private readonly ILog _log;
        private readonly IEventJsonSerializer _eventJsonSerializer;
        private readonly IMssqlConnection _connection;

        public EventStore(
            ILog log,
            IEventJsonSerializer eventJsonSerializer,
            IMssqlConnection connection)
        {
            _log = log;
            _eventJsonSerializer = eventJsonSerializer;
            _connection = connection;
        }

        public async Task<IReadOnlyCollection<IDomainEvent>> StoreAsync<TAggregate>(
            string id,
            int oldVersion,
            int newVersion,
            IReadOnlyCollection<IUncommittedDomainEvent> uncommittedDomainEvents)
            where TAggregate : IAggregateRoot
        {
            var batchId = Guid.NewGuid();
            var aggregateType = typeof (TAggregate);
            var aggregateName = aggregateType.Name.Replace("Aggregate", string.Empty);
            var eventDataModels = uncommittedDomainEvents
                .Select((e, i) => ToEventDataModel(e, batchId, id, aggregateName, oldVersion + 1 + i))
                .ToList();

            const string sql = @"
                INSERT INTO
                    EventSource
                        (BatchId, AggregateId, AggregateName, Data, Metadata, AggregateSequenceNumber)
                    VALUES
                        (@BatchId, @AggregateId, @AggregateName, @Data, @Metadata, @AggregateSequenceNumber);
                SELECT CAST(SCOPE_IDENTITY() as bigint);";

            var resultingDomainEvents = new List<IDomainEvent>();
            foreach (var eventDataModel in eventDataModels)
            {
                eventDataModel.GlobalSequenceNumber = (await _connection.QueryAsync<long>(sql, eventDataModel).ConfigureAwait(false)).Single();
                resultingDomainEvents.Add(ToDomainEvent(eventDataModel));
            }

            return resultingDomainEvents;
        }

        public async Task<IReadOnlyCollection<IDomainEvent>> LoadAsync(string id)
        {
            const string sql = @"SELECT * FROM EventSource WHERE AggregateId = @AggregateId ORDER BY AggregateSequenceNumber ASC";
            var eventDataModels = await _connection.QueryAsync<EventDataModel>(sql, new {AggregateId = id}).ConfigureAwait(false);
            var domainEvents = eventDataModels
                .Select(ToDomainEvent)
                .ToList();
            return domainEvents;
        }

        private IDomainEvent ToDomainEvent(ICommittedDomainEvent committedDomainEvent)
        {
            return _eventJsonSerializer.Deserialize(committedDomainEvent);
        }

        private EventDataModel ToEventDataModel(
            IUncommittedDomainEvent uncommittedDomainEvent,
            Guid batchId,
            string aggregateId,
            string aggregateName,
            int version)
        {
            var eventData = _eventJsonSerializer.Serialize(uncommittedDomainEvent);

            return new EventDataModel
                {
                    AggregateId = aggregateId,
                    AggregateName = aggregateName,
                    BatchId = batchId,
                    Data = eventData.Data,
                    Metadata = eventData.Meta,
                    AggregateSequenceNumber = version
                };
        }
    }
}
