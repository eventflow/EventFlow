using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.Snapshots;
using EventFlow.Snapshots.Stores;
using Redis.OM;
using Redis.OM.Searching;

namespace EventFlow.Redis.SnapshotStore;

public class RedisSnapshotPersistence : ISnapshotPersistence
{
    private readonly RedisConnectionProvider _provider;
    private readonly IRedisCollection<RedisSnapshot> _collection;

    public RedisSnapshotPersistence(RedisConnectionProvider provider)
    {
        _provider = provider;
        _collection = provider.RedisCollection<RedisSnapshot>();
    }

    public async Task<CommittedSnapshot> GetSnapshotAsync(Type aggregateType, IIdentity identity,
        CancellationToken cancellationToken)
    {
        var snapshot = await _collection.FirstOrDefaultAsync(sn => sn.AggregateId == identity.Value);

        return snapshot is null ? null : new CommittedSnapshot(snapshot.Metadata, snapshot.Data);
    }

    public async Task SetSnapshotAsync(Type aggregateType, IIdentity identity, SerializedSnapshot serializedSnapshot,
        CancellationToken cancellationToken)
    {
        var aggregateName = aggregateType.GetAggregateName().Value;
        var snapshot = new RedisSnapshot
        {
            Id = "test",
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
    }

    public async Task DeleteSnapshotAsync(Type aggregateType, IIdentity identity, CancellationToken cancellationToken)
    {
        var snapshot = await _collection.FirstOrDefaultAsync(sn => sn.AggregateId == identity.Value);
        if (snapshot is not null)
            await _collection.DeleteAsync(snapshot);
    }

    public async Task PurgeSnapshotsAsync(Type aggregateType, CancellationToken cancellationToken)
    {
        var aggregateName = aggregateType.GetAggregateName().Value;
        var snapshots = await _collection.Where(sn => sn.AggregateName == aggregateName).ToListAsync();
        await Task.WhenAll(snapshots.Select(sn => _collection.DeleteAsync(sn)));
    }

    public Task PurgeSnapshotsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = _provider.Connection.DropIndexAndAssociatedRecords(typeof(RedisSnapshot));
            if (!result)
            {
                //TODO log
            }

            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            //TODO log
            throw;
        }
    }
}