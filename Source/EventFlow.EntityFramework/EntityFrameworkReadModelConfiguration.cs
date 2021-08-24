using System.Linq;
using EventFlow.EntityFramework.ReadStores.Configuration;
using EventFlow.ReadStores;

namespace EventFlow.EntityFramework
{
    public sealed class EntityFrameworkReadModelConfiguration<TReadModel> : IApplyQueryableConfiguration<TReadModel>
        where TReadModel : class, IReadModel, new()
    {
        IQueryable<TReadModel>
            IApplyQueryableConfiguration<TReadModel>.Apply(IQueryable<TReadModel> queryable) =>
            queryable;
    }
}