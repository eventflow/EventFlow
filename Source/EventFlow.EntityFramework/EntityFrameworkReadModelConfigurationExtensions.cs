using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using EventFlow.EntityFramework.ReadStores.Configuration;
using EventFlow.EntityFramework.ReadStores.Configuration.Includes;
using EventFlow.ReadStores;

namespace EventFlow.EntityFramework
{
    /// <summary>
    /// Extensions methods to configure the ReadModel
    /// </summary>
    public static class EntityFrameworkReadModelConfigurationExtensions
    {
        /// <inheritdoc cref="Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include{TReadModel,TProperty}(System.Linq.IQueryable{TReadModel},Expression{Func{TReadModel, TProperty}})"/>
        public static IncludeExpression<TReadModel, TProperty>
            Include<TReadModel, TProperty>(
                this IApplyQueryableConfiguration<TReadModel> source,
                Expression<Func<TReadModel, TProperty>> navigationPropertyPath)
            where TReadModel : class, IReadModel, new()
        {
            if (navigationPropertyPath == null)
            {
                throw new ArgumentNullException(nameof(navigationPropertyPath));
            }

            return new IncludeExpression<TReadModel, TProperty>(
                source,
                navigationPropertyPath);
        }

        /// <inheritdoc cref="Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include{TReadModel}(System.Linq.IQueryable{TReadModel},string)"/>
        public static IncludeString<TReadModel>
            Include<TReadModel>(
                this IApplyQueryableConfiguration<TReadModel> source,
                string navigationPropertyPath)
            where TReadModel : class, IReadModel, new()
        {
            if (navigationPropertyPath == null)
            {
                throw new ArgumentNullException(nameof(navigationPropertyPath));
            }

            if (string.IsNullOrWhiteSpace(navigationPropertyPath))
            {
                throw new ArgumentException("Must not be null or empty", nameof(navigationPropertyPath));
            }

            return new IncludeString<TReadModel>(
                source, 
                navigationPropertyPath);
        }

        /// <inheritdoc cref="Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ThenInclude{TReadModel,TPreviousProperty,TProperty}(Microsoft.EntityFrameworkCore.Query.IIncludableQueryable{TReadModel,TPreviousProperty},Expression{Func{TPreviousProperty, TProperty}})"/>
        public static ThenIncludeExpression<TEntity, TPreviousProperty, TProperty>
            ThenInclude<TEntity, TPreviousProperty, TProperty>(
                this IApplyQueryableIncludeConfiguration<TEntity, TPreviousProperty> source,
                Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath)
            where TEntity : class, IReadModel, new()
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (navigationPropertyPath == null)
            {
                throw new ArgumentNullException(nameof(navigationPropertyPath));
            }

            return new ThenIncludeExpression<TEntity, TPreviousProperty, TProperty>(
                source,
                navigationPropertyPath);
        }

        /// <inheritdoc cref="Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ThenInclude{TReadModel,TPreviousProperty,TProperty}(Microsoft.EntityFrameworkCore.Query.IIncludableQueryable{TReadModel,IEnumerable{TPreviousProperty}},Expression{Func{TPreviousProperty, TProperty}})"/>
        public static ThenIncludeEnumerableExpression<TEntity, TPreviousProperty, TProperty>
            ThenInclude<TEntity, TPreviousProperty, TProperty>(
                this IApplyQueryableIncludeConfiguration<TEntity, IEnumerable<TPreviousProperty>> source,
                Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath)
            where TEntity : class, IReadModel, new()
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (navigationPropertyPath == null)
            {
                throw new ArgumentNullException(nameof(navigationPropertyPath));
            }

            return new ThenIncludeEnumerableExpression<TEntity, TPreviousProperty, TProperty>(
                source,
                navigationPropertyPath);
        }
    }
}