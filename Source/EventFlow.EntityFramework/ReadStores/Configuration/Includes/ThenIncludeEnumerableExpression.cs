using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EventFlow.ReadStores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace EventFlow.EntityFramework.ReadStores.Configuration.Includes
{
    public sealed class ThenIncludeEnumerableExpression<TReadModel, TPreviousProperty, TProperty>
        : IApplyQueryableIncludeConfiguration<TReadModel, TProperty>
        where TReadModel : class, IReadModel, new()
    {
        private readonly IApplyQueryableIncludeConfiguration<TReadModel, IEnumerable<TPreviousProperty>> _source;
        private readonly Expression<Func<TPreviousProperty, TProperty>> _navigationPropertyPath;

        public ThenIncludeEnumerableExpression(
            IApplyQueryableIncludeConfiguration<TReadModel, IEnumerable<TPreviousProperty>> source,
            Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath)
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
            _source
                .Apply(queryable)
                .ThenInclude(_navigationPropertyPath);
    }
}