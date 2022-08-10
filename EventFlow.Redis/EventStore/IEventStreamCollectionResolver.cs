namespace EventFlow.Redis.EventStore;

public interface IEventStreamCollectionResolver
{
    Task<IEnumerable<PrefixedKey>> GetStreamNamesAsync(CancellationToken cancellationToken = default);
}