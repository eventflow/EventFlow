using EventFlow.ReadStores;

namespace EventFlow.MongoDB.ReadStores
{
    public interface IMongoDbInsertOnlyReadModel : IReadModel
    {
        object _id { get; set; }
    }
}
