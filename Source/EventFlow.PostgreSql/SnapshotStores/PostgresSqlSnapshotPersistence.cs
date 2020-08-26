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

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.PostgreSql.Connections;
using EventFlow.Snapshots;
using EventFlow.Snapshots.Stores;
using Npgsql;

namespace EventFlow.PostgreSql.SnapshotStores
{
    public class PostgreSqlSnapshotPersistence : ISnapshotPersistence
    {
        private readonly ILog _log;
        private readonly IPostgreSqlConnection _postgreSqlConnection;

        public PostgreSqlSnapshotPersistence(
            ILog log,
            IPostgreSqlConnection postgreSqlConnection)
        {
            _log = log;
            _postgreSqlConnection = postgreSqlConnection;
        }

        public async Task<CommittedSnapshot> GetSnapshotAsync(
            Type aggregateType,
            IIdentity identity,
            CancellationToken cancellationToken)
        {
            var postgreSqlSnapshotDataModels = await _postgreSqlConnection.QueryAsync<PostgreSqlSnapshotDataModel>(
                Label.Named("fetch-snapshot"),
                cancellationToken,
                "SELECT * FROM EventFlowSnapshots " +
                "WHERE AggregateName = @AggregateName AND AggregateId = @AggregateId ORDER BY AggregateSequenceNumber DESC " +
                "LIMIT 1;",
                new {AggregateId = identity.Value, AggregateName = aggregateType.GetAggregateName().Value})
                .ConfigureAwait(false);

            if (!postgreSqlSnapshotDataModels.Any())
            {
                return null;
            }

            var postgreSqlSnapshotDataModel = postgreSqlSnapshotDataModels.Single();
            return new CommittedSnapshot(
                postgreSqlSnapshotDataModel.Metadata,
                postgreSqlSnapshotDataModel.Data);
        }

        public async Task SetSnapshotAsync(
            Type aggregateType,
            IIdentity identity,
            SerializedSnapshot serializedSnapshot,
            CancellationToken cancellationToken)
        {
            var postgreSqlSnapshotDataModel = new PostgreSqlSnapshotDataModel
            {
                    AggregateId = identity.Value,
                    AggregateName = aggregateType.GetAggregateName().Value,
                    AggregateSequenceNumber = serializedSnapshot.Metadata.AggregateSequenceNumber,
                    Metadata = serializedSnapshot.SerializedMetadata,
                    Data = serializedSnapshot.SerializedData,
                };

            try
            {
                await _postgreSqlConnection.ExecuteAsync(
                    Label.Named("set-snapshot"),
                    cancellationToken,
                    @"INSERT INTO EventFlowSnapshots
                        (AggregateId, AggregateName, AggregateSequenceNumber, Metadata, Data)
                        VALUES
                        (@AggregateId, @AggregateName, @AggregateSequenceNumber, @Metadata, @Data);",
                    postgreSqlSnapshotDataModel)
                    .ConfigureAwait(false);
            }
            catch (PostgresException sqlException) when (sqlException.SqlState == "23505")
            {
                //If we have a duplicate key exception, then the snapshot has already been created
                //https://www.postgresql.org/docs/9.4/static/errcodes-appendix.html

                _log.Debug("Duplicate key SQL exception : {0}", sqlException.MessageText);
            }
        }

        public Task DeleteSnapshotAsync(
            Type aggregateType,
            IIdentity identity,
            CancellationToken cancellationToken)
        {
            return _postgreSqlConnection.ExecuteAsync(
                Label.Named("delete-snapshots-for-aggregate"),
                cancellationToken,
                "DELETE FROM EventFlowSnapshots WHERE AggregateName = @AggregateName AND AggregateId = @AggregateId;",
                new {AggregateId = identity.Value, AggregateName = aggregateType.GetAggregateName().Value});
        }

        public Task PurgeSnapshotsAsync(
            Type aggregateType,
            CancellationToken cancellationToken)
        {
            return _postgreSqlConnection.ExecuteAsync(
                Label.Named("purge-snapshots-for-aggregate"),
                cancellationToken,
                "DELETE FROM EventFlowSnapshots WHERE AggregateName = @AggregateName;",
                new {AggregateName = aggregateType.GetAggregateName().Value});
        }

        public Task PurgeSnapshotsAsync(
            CancellationToken cancellationToken)
        {
            return _postgreSqlConnection.ExecuteAsync(
                Label.Named("purge-all-snapshots"),
                cancellationToken,
                "DELETE FROM EventFlowSnapshots;");
        }

        public class PostgreSqlSnapshotDataModel
        {
            public string AggregateId { get; set; }
            public string AggregateName { get; set; }
            public int AggregateSequenceNumber { get; set; }
            public string Data { get; set; }
            public string Metadata { get; set; }
        }
    }
}