using EventFlow.ReadStores;

namespace EventFlow.MongoDB.ReadStores.InsertOnly
{
    public interface IMongoDbInsertOnlyReadModelStore<TReadModel> : IReadModelStore<TReadModel>
        where TReadModel : class, IReadModel, new()
    {
    }
}
