using EventFlow.ReadStores;

namespace EventFlow.Firebase.ReadStores
{
    public interface IFirebaseReadModelStore<TReadModel> : IReadModelStore<TReadModel>
        where TReadModel : class, IReadModel, new()
    {
    }
}
