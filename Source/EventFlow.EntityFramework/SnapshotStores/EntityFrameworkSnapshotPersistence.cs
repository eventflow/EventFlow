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
using EventFlow.EntityFramework.Extensions;
using EventFlow.Extensions;
using EventFlow.Snapshots;
using EventFlow.Snapshots.Stores;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework.SnapshotStores
{
    public class EntityFrameworkSnapshotPersistence<TDbContext> : ISnapshotPersistence
        where TDbContext : DbContext
    {
        private readonly IDbContextProvider<TDbContext> _contextProvider;
        private readonly IUniqueConstraintDetectionStrategy _strategy;
        private readonly int _deletionBatchSize;

        public EntityFrameworkSnapshotPersistence(
            IBulkOperationConfiguration bulkOperationConfiguration,
            IDbContextProvider<TDbContext> contextProvider,
            IUniqueConstraintDetectionStrategy strategy
        )
        {
            _contextProvider = contextProvider;
            _strategy = strategy;
            _deletionBatchSize = bulkOperationConfiguration.DeletionBatchSize;
        }

        public async Task<CommittedSnapshot> GetSnapshotAsync(
            Type aggregateType,
            IIdentity identity,
            CancellationToken cancellationToken)
        {
            var aggregateName = aggregateType.GetAggregateName().Value;

            using (var dbContext = _contextProvider.CreateContext())
            {
                var snapshot = await dbContext.Set<SnapshotEntity>()
                    .AsNoTracking()
                    .Where(s => s.AggregateName == aggregateName
                                && s.AggregateId == identity.Value)
                    .OrderByDescending(s => s.AggregateSequenceNumber)
                    .Select(s => new SnapshotEntity
                    {
                        Metadata = s.Metadata,
                        Data = s.Data
                    })
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);

                return snapshot == null 
                    ? null 
                    : new CommittedSnapshot(snapshot.Metadata, snapshot.Data);
            }
        }

        public async Task SetSnapshotAsync(
            Type aggregateType,
            IIdentity identity,
            SerializedSnapshot serializedSnapshot,
            CancellationToken cancellationToken)
        {
            var entity = new SnapshotEntity
            {
                AggregateId = identity.Value,
                AggregateName = aggregateType.GetAggregateName().Value,
                AggregateSequenceNumber = serializedSnapshot.Metadata.AggregateSequenceNumber,
                Metadata = serializedSnapshot.SerializedMetadata,
                Data = serializedSnapshot.SerializedData
            };

            using (var dbContext = _contextProvider.CreateContext())
            {
                dbContext.Add(entity);

                try
                {
                    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation(_strategy))
                {
                    // If we have a duplicate key exception, then the snapshot has already been created
                }
            }
        }

        public Task DeleteSnapshotAsync(
            Type aggregateType,
            IIdentity identity,
            CancellationToken cancellationToken)
        {
            var aggregateName = aggregateType.GetAggregateName().Value;
            var aggregateId = identity.Value;
            return Bulk.Delete<TDbContext, SnapshotEntity, SnapshotEntity>(
                _contextProvider,
                _deletionBatchSize,
                cancellationToken,
                e => new SnapshotEntity {Id = e.Id},
                e => e.AggregateName == aggregateName
                     && e.AggregateId == aggregateId);
        }

        public Task PurgeSnapshotsAsync(
            Type aggregateType,
            CancellationToken cancellationToken)
        {
            var aggregateName = aggregateType.GetAggregateName().Value;
            return Bulk.Delete<TDbContext, SnapshotEntity, SnapshotEntity>(
                _contextProvider,
                _deletionBatchSize,
                cancellationToken,
                e => new SnapshotEntity {Id = e.Id},
                e => e.AggregateName == aggregateName);
        }

        public Task PurgeSnapshotsAsync(
            CancellationToken cancellationToken)
        {
            return Bulk.Delete<TDbContext, SnapshotEntity, SnapshotEntity>(
                _contextProvider,
                _deletionBatchSize,
                cancellationToken, 
                e => new SnapshotEntity { Id = e.Id });
        }
    }
}

