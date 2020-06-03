﻿// The MIT License (MIT)
// 
// Copyright (c) 2015-2019 Rasmus Mikkelsen
// Copyright (c) 2015-2019 eBay Software Foundation
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Exceptions;
using EventFlow.Logs;
using EventFlow.PostgreSql.Connections;
using Npgsql;

namespace EventFlow.PostgreSql.EventStores
{
    public class PostgreSqlEventPersistence : PostgreSqlEventPersistence<string>, IEventPersistence
    {
        public PostgreSqlEventPersistence(ILog log, IPostgreSqlConnection connection)
            : base(log, connection)
        {
        }
    }

    public class PostgreSqlEventPersistence<TSerialized> : IEventPersistence<TSerialized>
    {
        private class EventDataModel : ICommittedDomainEvent<TSerialized>
        {
            public long GlobalSequenceNumber { get; set; }
            public Guid BatchId { get; set; }
            public string AggregateId { get; set; }
            public string AggregateName { get; set; }
            public TSerialized Data { get; set; }
            public TSerialized Metadata { get; set; }
            public int AggregateSequenceNumber { get; set; }
        }

        private readonly ILog _log;
        private readonly IPostgreSqlConnection _connection;

        public PostgreSqlEventPersistence(
            ILog log,
            IPostgreSqlConnection connection)
        {
            _log = log;
            _connection = connection;
        }

        public async Task<AllCommittedEventsPage<TSerialized>> LoadAllCommittedEvents(
            GlobalPosition globalPosition,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var startPosition = globalPosition.IsStart
                ? 0
                : long.Parse(globalPosition.Value);

            const string sql = @"
                SELECT
                    GlobalSequenceNumber, BatchId, AggregateId, AggregateName, Data, Metadata, AggregateSequenceNumber
                FROM EventFlow
                WHERE
                    GlobalSequenceNumber >= @startPosition
                ORDER BY
                    GlobalSequenceNumber ASC
                LIMIT @pageSize;";
            var eventDataModels = await _connection.QueryAsync<EventDataModel>(
                    Label.Named("postgresql-fetch-events"),
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

            return new AllCommittedEventsPage<TSerialized>(new GlobalPosition(nextPosition.ToString()), eventDataModels);
        }

        public async Task<IReadOnlyCollection<ICommittedDomainEvent<TSerialized>>> CommitEventsAsync(
            IIdentity id,
            IReadOnlyCollection<SerializedEvent<TSerialized>> serializedEvents,
            CancellationToken cancellationToken)
        {
            if (!serializedEvents.Any())
            {
                return new ICommittedDomainEvent<TSerialized>[] { };
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

            _log.Verbose(
                "Committing {0} events to PostgreSQL event store for entity with ID '{1}'",
                eventDataModels.Count,
                id);

            const string sql = @"
                INSERT INTO
                    EventFlow
                        (BatchId, AggregateId, AggregateName, Data, Metadata, AggregateSequenceNumber)
                    VALUES
                        (@BatchId, @AggregateId, @AggregateName, @Data, @Metadata, @AggregateSequenceNumber)
                 RETURNING GlobalSequenceNumber;";

            IReadOnlyCollection<long> ids;

            try
            {
                ids = await _connection.InsertMultipleAsync<long, EventDataModel>(
                    Label.Named("postgresql-insert-events"),
                    cancellationToken,
                    sql,
                    eventDataModels)
                    .ConfigureAwait(false);
            }
            catch (PostgresException e)
            {
                //If we have a duplicate key exception, then the event has already been created
                //https://www.postgresql.org/docs/9.4/static/errcodes-appendix.html

                if (e.SqlState == "23505") 
                {
                    throw new OptimisticConcurrencyException(e.Message, e);
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

        public async Task<IReadOnlyCollection<ICommittedDomainEvent<TSerialized>>> LoadCommittedEventsAsync(
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
                    AggregateSequenceNumber ASC;";
            var eventDataModels = await _connection.QueryAsync<EventDataModel>(
                Label.Named("postgresql-fetch-events"),
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

        public async Task DeleteEventsAsync(
            IIdentity id,
            CancellationToken cancellationToken)
        {
            const string sql = @"DELETE FROM EventFlow WHERE AggregateId = @AggregateId;";
            var affectedRows = await _connection.ExecuteAsync(
                Label.Named("postgresql-delete-aggregate"),
                cancellationToken,
                sql,
                new { AggregateId = id.Value })
                .ConfigureAwait(false);

            _log.Verbose(
                "Deleted entity with ID '{0}' by deleting all of its {1} events",
                id,
                affectedRows);
        }
    }
}