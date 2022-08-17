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
    private readonly RedisConnectionProvider _provider;
    private readonly IRedisCollection<RedisSnapshot> _collection;
    private readonly ILogger<RedisSnapshotPersistence> _logger;

    public RedisSnapshotPersistence(RedisConnectionProvider provider, ILogger<RedisSnapshotPersistence> logger)
    {
        _provider = provider;
        _logger = logger;
        _collection = provider.RedisCollection<RedisSnapshot>();
    }

    public async Task<CommittedSnapshot> GetSnapshotAsync(Type aggregateType, IIdentity identity,
        CancellationToken cancellationToken)
    {
        var snapshot = await _collection.FirstOrDefaultAsync(sn => sn.AggregateId == identity.Value);
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
        await Task.WhenAll(deleteTasks);

        await _collection.InsertAsync(snapshot);

        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("Saved snapshot with id {Id}", identity.Value);
    }

    public async Task DeleteSnapshotAsync(Type aggregateType, IIdentity identity, CancellationToken cancellationToken)
    {
        var snapshot = await _collection.FirstOrDefaultAsync(sn => sn.AggregateId == identity.Value);
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
        var snapshots = await _collection.Where(sn => sn.AggregateName == aggregateName).ToListAsync();
        await Task.WhenAll(snapshots.Select(sn => _collection.DeleteAsync(sn)));

        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("Purged all snapshots of aggregate {Aggregate}", aggregateName);
    }

    public Task PurgeSnapshotsAsync(CancellationToken cancellationToken)
    {
        var result = _provider.Connection.DropIndexAndAssociatedRecords(typeof(RedisSnapshot));
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            if (!result)
                _logger.LogTrace("Failed to purge all snapshots");
            else
                _logger.LogTrace("Purged all snapshots");
        }

        return Task.CompletedTask;
    }
}