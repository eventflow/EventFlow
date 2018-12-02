using EventFlow.ReadStores;

namespace EventFlow.MongoDB.ReadStores
{
    public interface IMongoDbReadModel : IReadModel
    {
        string Id { get; }
        long? Version { get; set; }
    }
}
