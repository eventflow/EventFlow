using EventFlow.ReadStores;

namespace EventFlow.MongoDB.ReadStores
{
    public interface IMongoDbInsertOnlyReadModel : IReadModel
    {
        object Id { get; set; }
    }
}
