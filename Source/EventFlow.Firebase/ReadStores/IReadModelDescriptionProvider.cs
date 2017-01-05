using EventFlow.Firebase.ValueObjects;
using EventFlow.ReadStores;

namespace EventFlow.Firebase.ReadStores
{
    public interface IReadModelDescriptionProvider
    {
        ReadModelDescription GetReadModelDescription<TReadModel>()
            where TReadModel : IReadModel;
    }
}
