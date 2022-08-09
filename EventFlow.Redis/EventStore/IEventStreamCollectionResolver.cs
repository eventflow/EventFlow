namespace EventFlow.Redis.EventStore;

public interface IEventStreamCollectionResolver
{
    Task<IEnumerable<string>> GetStreamNamesAsync(CancellationToken cancellationToken = default);
}