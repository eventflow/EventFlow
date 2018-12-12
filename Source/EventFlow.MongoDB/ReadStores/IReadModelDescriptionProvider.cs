using EventFlow.MongoDB.ValueObjects;

namespace EventFlow.MongoDB.ReadStores
{
    public interface IReadModelDescriptionProvider
    {
        ReadModelDescription GetReadModelDescription<TReadModel>()
            where TReadModel : IMongoDbReadModel;
    }
}
