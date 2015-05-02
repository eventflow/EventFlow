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
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.EventCaches;
using EventFlow.Exceptions;
using EventFlow.Logs;
using EventFlow.MsSql;

namespace EventFlow.EventStores.MsSql
{
    public class MsSqlEventStore : EventStore
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

        private readonly IMsSqlConnection _connection;

        public MsSqlEventStore(
            ILog log,
            IAggregateFactory aggregateFactory,
            IEventJsonSerializer eventJsonSerializer,
            IEventUpgradeManager eventUpgradeManager,
            IEnumerable<IMetadataProvider> metadataProviders,
            IEventCache eventCache,
            IMsSqlConnection connection)
            : base(log, aggregateFactory, eventJsonSerializer, eventCache, eventUpgradeManager, metadataProviders)
        {
            _connection = connection;
        }

        protected override async Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync<TAggregate>(
            string id,
            IReadOnlyCollection<SerializedEvent> serializedEvents,
            CancellationToken cancellationToken)
        {
            if (!serializedEvents.Any())
            {
                return new ICommittedDomainEvent[] {};
            }

            var batchId = Guid.NewGuid();
            var aggregateType = typeof(TAggregate);
            var aggregateName = aggregateType.Name.Replace("Aggregate", string.Empty);
            var eventDataModels = serializedEvents
                .Select((e, i) => new EventDataModel
                    {
                        AggregateId = id,
                        AggregateName = aggregateName,
                        BatchId = batchId,
                        Data = e.Data,
                        Metadata = e.Meta,
                        AggregateSequenceNumber = e.AggregateSequenceNumber,
                    })
                .ToList();

            Log.Verbose(
                "Committing {0} events to MSSQL event store for aggregate {1} with ID '{2}'",
                eventDataModels.Count,
                aggregateType.Name,
                id);

            const string sql = @"
                INSERT INTO
                    EventFlow
                        (BatchId, AggregateId, AggregateName, Data, Metadata, AggregateSequenceNumber)
                        OUTPUT CAST(INSERTED.GlobalSequenceNumber as bigint)
                    SELECT
                        BatchId, AggregateId, AggregateName, Data, Metadata, AggregateSequenceNumber
                    FROM
                        @rows
                    ORDER BY AggregateSequenceNumber ASC";

            IReadOnlyCollection<long> ids;
            try
            {
                ids = await _connection.InsertMultipleAsync<long, EventDataModel>(
                    cancellationToken,
                    sql,
                    eventDataModels)
                    .ConfigureAwait(false);
            }
            catch (SqlException exception)
            {
                if (exception.Number == 2601)
                {
                    Log.Verbose(
                        "MSSQL event insert detected an optimistic concurrency exception for aggregate '{0}' with ID '{1}'",
                        aggregateType.Name,
                        id);
                    throw new OptimisticConcurrencyException(exception.Message, exception);
                }

                throw;
            }

            eventDataModels = eventDataModels
                .Zip(
                    ids,
                    (e, i) =>
                        {
                            e.GlobalSequenceNumber = i;
                            return e;
                        })
                .ToList();

            return eventDataModels;
        }

        protected override async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync<TAggregate>(
            string id,
            CancellationToken cancellationToken)
        {
            const string sql = @"SELECT * FROM EventFlow WHERE AggregateId = @AggregateId ORDER BY AggregateSequenceNumber ASC";
            var eventDataModels = await _connection.QueryAsync<EventDataModel>(cancellationToken, sql, new { AggregateId = id })
                .ConfigureAwait(false);
            return eventDataModels;
        }

        protected async override Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync<TAggregate>(
            string id,
            int fromAggregateSequenceNumber,
            int toAggregateSequenceNumber,
            CancellationToken cancellationToken)
        {
            const string sql = @"
                SELECT *
                FROM
                    EventFlow
                WHERE
                    AggregateId = @AggregateId AND 
                    AggregateSequenceNumber >= @From AND 
                    AggregateSequenceNumber <= @To
                ORDER BY
                    AggregateSequenceNumber ASC";
            var eventDataModels = await _connection.QueryAsync<EventDataModel>(
                cancellationToken,
                sql,
                new
                    {
                        AggregateId = id,
                        From = fromAggregateSequenceNumber,
                        To = toAggregateSequenceNumber,
                    })
                .ConfigureAwait(false);
            return eventDataModels;
        }
    }
}
