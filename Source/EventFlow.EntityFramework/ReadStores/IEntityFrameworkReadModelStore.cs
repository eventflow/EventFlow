using EventFlow.ReadStores;

namespace EventFlow.EntityFramework.ReadStores
{
    public interface IEntityFrameworkReadModelStore<T> : IReadModelStore<T>
        where T : class, IReadModel
    {
    }
}
