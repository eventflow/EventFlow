using EventFlow.MongoDB.ReadStores;
using EventFlow.MongoDB.ValueObjects;

namespace EventFlow.Redis.ReadStore;

public class ReadModelDescriptionProvider : IReadModelDescriptionProvider
{
    public ReadModelDescription GetReadModelDescription<TReadModel>() where TReadModel : IMongoDbReadModel
    {
        throw new NotImplementedException();
    }
}