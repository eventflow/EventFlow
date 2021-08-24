using System.Linq;
using EventFlow.ReadStores;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework.ReadStores.Configuration.Includes
{
    public sealed class IncludeString<TReadModel>
        : IApplyQueryableConfiguration<TReadModel>
        where TReadModel : class, IReadModel, new()
    {
        private readonly IApplyQueryableConfiguration<TReadModel> _source;
        private readonly string _navigationPropertyPath;

        internal IncludeString(
            IApplyQueryableConfiguration<TReadModel> source,
            string navigationPropertyPath)
        {
            _source = source;
            _navigationPropertyPath = navigationPropertyPath;
        }

        IQueryable<TReadModel> IApplyQueryableConfiguration<TReadModel>.Apply(
            IQueryable<TReadModel> queryable) =>
            _source.Apply(queryable).Include(_navigationPropertyPath);
    }
}