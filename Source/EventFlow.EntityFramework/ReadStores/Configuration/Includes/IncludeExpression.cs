using System;
using System.Linq;
using System.Linq.Expressions;
using EventFlow.ReadStores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace EventFlow.EntityFramework.ReadStores.Configuration.Includes
{
    public sealed class IncludeExpression<TReadModel, TProperty>
        : IApplyQueryableIncludeConfiguration<TReadModel, TProperty>
        where TReadModel : class, IReadModel, new()
    {
        private readonly IApplyQueryableConfiguration<TReadModel> _source;
        private readonly Expression<Func<TReadModel, TProperty>> _navigationPropertyPath;

        internal IncludeExpression(
            IApplyQueryableConfiguration<TReadModel> source,
            Expression<Func<TReadModel, TProperty>> navigationPropertyPath)
        {
            _source = source;
            _navigationPropertyPath = navigationPropertyPath;
        }

        IQueryable<TReadModel>
            IApplyQueryableConfiguration<TReadModel>.Apply(
                IQueryable<TReadModel> queryable) => ApplyInternal(queryable);

        IIncludableQueryable<TReadModel, TProperty>
            IApplyQueryableIncludeConfiguration<TReadModel, TProperty>.Apply(
                IQueryable<TReadModel> queryable) => ApplyInternal(queryable);

        private IIncludableQueryable<TReadModel, TProperty> ApplyInternal(IQueryable<TReadModel> queryable) =>
            _source.Apply(queryable).Include(_navigationPropertyPath);
    }
}