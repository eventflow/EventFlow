using EventFlow.ReadStores;

namespace EventFlow.MongoDB.ReadStores.InsertOnly
{
    public interface IMongoDbInsertOnlyReadModel : IReadModel
    {
        object Id { get; set; }
    }
}
