using EventFlow.ReadStores;
using Redis.OM.Modeling;

namespace EventFlow.Redis.ReadStore;

[Document(Prefixes = new[] {Constants.ReadModelPrefix})]
public abstract class RedisReadModel : IReadModel
{
    [RedisIdField] public string Id { get; set; }
    [Indexed] public long? Version { get; set; }
}