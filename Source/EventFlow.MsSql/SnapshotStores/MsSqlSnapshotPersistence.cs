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
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.Snapshots;
using EventFlow.Snapshots.Stores;

namespace EventFlow.MsSql.SnapshotStores
{
    public class MsSqlSnapshotPersistence : ISnapshotPersistence
    {
        private readonly IMsSqlConnection _msSqlConnection;

        public MsSqlSnapshotPersistence(
            IMsSqlConnection msSqlConnection)
        {
            _msSqlConnection = msSqlConnection;
        }

        public async Task<CommittedSnapshot> GetSnapshotAsync(
            Type aggregateType,
            IIdentity identity,
            CancellationToken cancellationToken)
        {
            var msSqlSnapshotDataModels = await _msSqlConnection.QueryAsync<MsSqlSnapshotDataModel>(
                Label.Named("fetch-snapshot"),
                cancellationToken,
                "SELECT TOP 1 * FROM [dbo].[EventFlowSnapshots] WHERE AggregateName = @AggregateName AND AggregateId = @AggregateId ORDER BY AggregateSequenceNumber DESC",
                new {AggregateId = identity.Value, AggregateName = aggregateType.GetAggregateName().Value})
                .ConfigureAwait(false);

            if (!msSqlSnapshotDataModels.Any())
            {
                return null;
            }

            var msSqlSnapshotDataModel = msSqlSnapshotDataModels.Single();
            return new CommittedSnapshot(
                msSqlSnapshotDataModel.Metadata,
                msSqlSnapshotDataModel.Data);
        }

        public async Task SetSnapshotAsync(
            Type aggregateType,
            IIdentity identity,
            SerializedSnapshot serializedSnapshot,
            CancellationToken cancellationToken)
        {
            var msSqlSnapshotDataModel = new MsSqlSnapshotDataModel
                {
                    AggregateId = identity.Value,
                    AggregateName = aggregateType.GetAggregateName().Value,
                    AggregateSequenceNumber = serializedSnapshot.Metadata.AggregateSequenceNumber,
                    Metadata = serializedSnapshot.SerializedMetadata,
                    Data = serializedSnapshot.SerializedData,
                };

            try
            {
                await _msSqlConnection.ExecuteAsync(
                    Label.Named("set-snapshot"),
                    cancellationToken,
                    @"INSERT INTO [dbo].[EventFlowSnapshots]
                        (AggregateId, AggregateName, AggregateSequenceNumber, Metadata, Data)
                        VALUES
                        (@AggregateId, @AggregateName, @AggregateSequenceNumber, @Metadata, @Data)",
                    msSqlSnapshotDataModel)
                    .ConfigureAwait(false);
            }
            catch (SqlException sqlException) when (sqlException.Number == 2601)
            {
                // If we have a duplicate key exception, then the snapshot has already been created
            }
        }

        public Task DeleteSnapshotAsync(
            Type aggregateType,
            IIdentity identity,
            CancellationToken cancellationToken)
        {
            return _msSqlConnection.ExecuteAsync(
                Label.Named("delete-snapshots-for-aggregate"),
                cancellationToken,
                "DELETE FROM [dbo].[EventFlowSnapshots] WHERE AggregateName = @AggregateName AND AggregateId = @AggregateId",
                new {AggregateId = identity.Value, AggregateName = aggregateType.GetAggregateName().Value});
        }

        public Task PurgeSnapshotsAsync(
            Type aggregateType,
            CancellationToken cancellationToken)
        {
            return _msSqlConnection.ExecuteAsync(
                Label.Named("purge-snapshots-for-aggregate"),
                cancellationToken,
                "DELETE FROM [dbo].[EventFlowSnapshots] WHERE AggregateName = @AggregateName",
                new {AggregateName = aggregateType.GetAggregateName().Value});
        }

        public Task PurgeSnapshotsAsync(
            CancellationToken cancellationToken)
        {
            return _msSqlConnection.ExecuteAsync(
                Label.Named("purge-all-snapshots"),
                cancellationToken,
                "DELETE FROM [dbo].[EventFlowSnapshots]");
        }

        public class MsSqlSnapshotDataModel
        {
            public string AggregateId { get; set; }
            public string AggregateName { get; set; }
            public int AggregateSequenceNumber { get; set; }
            public string Data { get; set; }
            public string Metadata { get; set; }
        }
    }
}