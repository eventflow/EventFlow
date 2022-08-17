namespace EventFlow.Redis.EventStore;

public interface IEventStreamCollectionResolver
{
    /// <summary>
    /// Returns the ids of all streams (aggregates) used by eventflow.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IEnumerable<PrefixedKey>> GetStreamIdsAsync(CancellationToken cancellationToken = default);
}