using EventFlow.ReadStores;

namespace EventFlow.MongoDB.ReadStores
{
    public interface IMongoDbInsertOnlyReadModelStore<TReadModel> : IReadModelStore<TReadModel>
        where TReadModel : class, IReadModel, new()
    {
    }
}
