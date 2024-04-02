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

using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.Snapshots;
using EventFlow.Snapshots.Stores;
using Microsoft.Extensions.Logging;
using Redis.OM;
using Redis.OM.Searching;

namespace EventFlow.Redis.SnapshotStore;

public class RedisSnapshotPersistence : ISnapshotPersistence
{
    private readonly IRedisCollection<RedisSnapshot> _collection;
    private readonly ILogger<RedisSnapshotPersistence> _logger;
    private readonly RedisConnectionProvider _provider;

    public RedisSnapshotPersistence(RedisConnectionProvider provider, ILogger<RedisSnapshotPersistence> logger)
    {
        _provider = provider;
        _logger = logger;
        _collection = provider.RedisCollection<RedisSnapshot>();
    }

    public async Task<CommittedSnapshot> GetSnapshotAsync(Type aggregateType, IIdentity identity,
        CancellationToken cancellationToken)
    {
        var snapshot = await _collection.FirstOrDefaultAsync(sn => sn.AggregateId == identity.Value)
            .ConfigureAwait(false);
        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("Found snapshot for id {Id}: {Snapshot}", identity.Value, snapshot);

        return snapshot is null ? null : new CommittedSnapshot(snapshot.Metadata, snapshot.Data);
    }

    public async Task SetSnapshotAsync(Type aggregateType, IIdentity identity, SerializedSnapshot serializedSnapshot,
        CancellationToken cancellationToken)
    {
        var aggregateName = aggregateType.GetAggregateName().Value;
        var snapshot = new RedisSnapshot
        {
            Id = SnapshotId.New.Value,
            AggregateId = identity.Value,
            AggregateName = aggregateName,
            AggregateSequenceNumber = serializedSnapshot.Metadata.AggregateSequenceNumber,
            Data = serializedSnapshot.SerializedData,
            Metadata = serializedSnapshot.SerializedMetadata
        };


        var prevSnapshots = await _collection.Where(sn =>
            sn.AggregateId == identity.Value && sn.AggregateName == aggregateName && sn.AggregateSequenceNumber ==
            serializedSnapshot.Metadata.AggregateSequenceNumber).ToListAsync();

        var deleteTasks = prevSnapshots.Select(pr => _collection.DeleteAsync(pr));
        await Task.WhenAll(deleteTasks).ConfigureAwait(false);

        await _collection.InsertAsync(snapshot).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("Saved snapshot with id {Id}", identity.Value);
    }

    public async Task DeleteSnapshotAsync(Type aggregateType, IIdentity identity, CancellationToken cancellationToken)
    {
        var snapshot = await _collection.FirstOrDefaultAsync(sn => sn.AggregateId == identity.Value)
            .ConfigureAwait(false);
        if (snapshot is not null)
        {
            await _collection.DeleteAsync(snapshot);
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace("Deleted snapshot with id {Id}", identity.Value);
        }
        else
            _logger.LogTrace("Failed to delete snapshot with id {Id}, snapshot was not found", identity.Value);
    }

    public async Task PurgeSnapshotsAsync(Type aggregateType, CancellationToken cancellationToken)
    {
        var aggregateName = aggregateType.GetAggregateName().Value;
        var snapshots = await _collection.Where(sn => sn.AggregateName == aggregateName).ToListAsync()
            .ConfigureAwait(false);
        await Task.WhenAll(snapshots.Select(sn => _collection.DeleteAsync(sn))).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("Purged all snapshots of aggregate {Aggregate}", aggregateName);
    }

    public Task PurgeSnapshotsAsync(CancellationToken cancellationToken)
    {
        var result = _provider.Connection.DropIndexAndAssociatedRecords(typeof(RedisSnapshot));
        if (!result)
            _logger.LogWarning("Failed to purge all snapshots");
        else if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("Purged all snapshots");

        return Task.CompletedTask;
    }
}