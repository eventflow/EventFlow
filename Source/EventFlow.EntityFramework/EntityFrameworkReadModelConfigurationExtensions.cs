// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
// https://github.com/eventflow/EventFlow
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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