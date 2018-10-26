using EventFlow.MongoDB.ValueObjects;

namespace EventFlow.MongoDB.ReadStores.InsertOnly
{
    public interface IInsertOnlyReadModelDescriptionProvider
    {
        ReadModelDescription GetReadModelDescription<TReadModel>()
            where TReadModel : IMongoDbInsertOnlyReadModel;
    }
}
