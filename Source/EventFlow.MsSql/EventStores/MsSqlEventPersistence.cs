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

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Exceptions;
using Microsoft.Extensions.Logging;

namespace EventFlow.MsSql.EventStores
{
    public class MsSqlEventPersistence : IEventPersistence
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

        private readonly ILogger<MsSqlEventPersistence> _logger;
        private readonly IMsSqlConnection _connection;

        public MsSqlEventPersistence(
            ILogger<MsSqlEventPersistence> logger,
            IMsSqlConnection connection)
        {
            _logger = logger;
            _connection = connection;
        }

        public async Task<AllCommittedEventsPage> LoadAllCommittedEvents(
            GlobalPosition globalPosition,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var startPosition = globalPosition.IsStart
                ? 0
                : long.Parse(globalPosition.Value);

            const string sql = @"
                SELECT TOP(@pageSize)
                    GlobalSequenceNumber, BatchId, AggregateId, AggregateName, Data, Metadata, AggregateSequenceNumber
                FROM EventFlow
                WHERE
                    GlobalSequenceNumber >= @startPosition
                ORDER BY
                    GlobalSequenceNumber ASC";
            var eventDataModels = await _connection.QueryAsync<EventDataModel>(
                Label.Named("mssql-fetch-events"),
                null,
                cancellationToken,
                sql,
                new
                {
                    startPosition,
                    pageSize
                })
                .ConfigureAwait(false);

            var nextPosition = eventDataModels.Any()
                ? eventDataModels.Max(e => e.GlobalSequenceNumber) + 1
                : startPosition;

            return new AllCommittedEventsPage(new GlobalPosition(nextPosition.ToString()), eventDataModels);
        }

        public async Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync(
            IIdentity id,
            IReadOnlyCollection<SerializedEvent> serializedEvents,
            CancellationToken cancellationToken)
        {
            if (!serializedEvents.Any())
            {
                return new ICommittedDomainEvent[] {};
            }

            var eventDataModels = serializedEvents
                .Select((e, i) => new EventDataModel
                    {
                        AggregateId = id.Value,
                        AggregateName = e.Metadata[MetadataKeys.AggregateName],
                        BatchId = Guid.Parse(e.Metadata[MetadataKeys.BatchId]),
                        Data = e.SerializedData,
                        Metadata = e.SerializedMetadata,
                        AggregateSequenceNumber = e.AggregateSequenceNumber,
                    })
                .ToList();

            _logger.LogTrace(
                "Committing {EventCount} events to MSSQL event store for aggregate with ID {AggregateId}",
                eventDataModels.Count,
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
                    Label.Named("mssql-insert-events"),
                    null, 
                    cancellationToken,
                    sql,
                    eventDataModels)
                    .ConfigureAwait(false);
            }
            catch (SqlException exception)
            {
                if (exception.Number == 2601)
                {
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

        public async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(
            IIdentity id,
            int fromEventSequenceNumber,
            CancellationToken cancellationToken)
        {
            const string sql = @"
                SELECT
                    GlobalSequenceNumber, BatchId, AggregateId, AggregateName, Data, Metadata, AggregateSequenceNumber
                FROM EventFlow
                WHERE
                    AggregateId = @AggregateId AND
                    AggregateSequenceNumber >= @FromEventSequenceNumber
                ORDER BY
                    AggregateSequenceNumber ASC";
            var eventDataModels = await _connection.QueryAsync<EventDataModel>(
                Label.Named("mssql-fetch-events"),
                null, 
                cancellationToken,
                sql,
                new
                {
                    AggregateId = id.Value,
                    FromEventSequenceNumber = fromEventSequenceNumber,
                })
                .ConfigureAwait(false);
            return eventDataModels;
        }

        public async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(
            IIdentity id,
            int fromEventSequenceNumber,
            int toEventSequenceNumber,
            CancellationToken cancellationToken)
        {
            const string sql = @"
                SELECT
                    GlobalSequenceNumber, BatchId, AggregateId, AggregateName, Data, Metadata, AggregateSequenceNumber
                FROM EventFlow
                WHERE
                    AggregateId = @AggregateId AND
                    AggregateSequenceNumber >= @FromEventSequenceNumber AND
                    AggregateSequenceNumber <= @ToEventSequenceNumber
                ORDER BY
                    AggregateSequenceNumber ASC";
            var eventDataModels = await _connection.QueryAsync<EventDataModel>(
                    Label.Named("mssql-fetch-events"),
                    null, 
                    cancellationToken,
                    sql,
                    new
                    {
                        AggregateId = id.Value,
                        FromEventSequenceNumber = fromEventSequenceNumber,
                        ToEventSequenceNumber = toEventSequenceNumber
                    })
                .ConfigureAwait(false);
            return eventDataModels;
        }

        public async Task DeleteEventsAsync(IIdentity id, CancellationToken cancellationToken)
        {
            const string sql = @"DELETE FROM EventFlow WHERE AggregateId = @AggregateId";
            var affectedRows = await _connection.ExecuteAsync(
                Label.Named("mssql-delete-aggregate"),
                null,
                cancellationToken,
                sql,
                new { AggregateId = id.Value })
                .ConfigureAwait(false);

            _logger.LogTrace(
                "Deleted aggregate with ID {AggregateId} by deleting all of its {EventCount} events",
                id,
                affectedRows);
        }
    }
}