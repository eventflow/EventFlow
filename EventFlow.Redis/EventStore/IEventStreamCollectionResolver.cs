namespace EventFlow.Redis.EventStore;

internal interface IEventStreamCollectionResolver
{
    /// <summary>
    /// Returns the ids of all streams (aggregates) used by eventflow.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyCollection<PrefixedKey>> GetStreamIdsAsync(CancellationToken cancellationToken = default);
}