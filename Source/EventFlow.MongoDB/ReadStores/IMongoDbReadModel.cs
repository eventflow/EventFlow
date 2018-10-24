using EventFlow.ReadStores;

namespace EventFlow.MongoDB.ReadStores
{
    public interface IMongoDbReadModel : IReadModel
    {
        string _id { get; }
        long? _version { get; set; }
    }
}
