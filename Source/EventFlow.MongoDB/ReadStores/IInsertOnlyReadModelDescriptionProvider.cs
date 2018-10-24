using EventFlow.MongoDB.ValueObjects;

namespace EventFlow.MongoDB.ReadStores
{
    public interface IInsertOnlyReadModelDescriptionProvider
    {
        ReadModelDescription GetReadModelDescription<TReadModel>()
            where TReadModel : IMongoDbInsertOnlyReadModel;
    }
}
