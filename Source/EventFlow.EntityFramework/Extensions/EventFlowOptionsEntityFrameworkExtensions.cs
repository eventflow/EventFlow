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
using EventFlow.Configuration;
using EventFlow.EntityFramework.EventStores;
using EventFlow.EntityFramework.ReadStores;
using EventFlow.EntityFramework.ReadStores.Configuration;
using EventFlow.EntityFramework.ReadStores.Configuration.Includes;
using EventFlow.EntityFramework.SnapshotStores;
using EventFlow.Extensions;
using EventFlow.ReadStores;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework.Extensions
{
    public static class EventFlowOptionsEntityFrameworkExtensions
    {
        public static IEventFlowOptions ConfigureEntityFramework(
            this IEventFlowOptions eventFlowOptions,
            IEntityFrameworkConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            return eventFlowOptions.RegisterServices(configuration.Apply);
        }

        public static IEventFlowOptions UseEntityFrameworkEventStore<TDbContext>(
            this IEventFlowOptions eventFlowOptions)
            where TDbContext : DbContext
        {
            return eventFlowOptions
                .UseEventStore<EntityFrameworkEventPersistence<TDbContext>>();
        }

        public static IEventFlowOptions UseEntityFrameworkSnapshotStore<TDbContext>(
            this IEventFlowOptions eventFlowOptions)
            where TDbContext : DbContext
        {
            return eventFlowOptions
                .UseSnapshotStore<EntityFrameworkSnapshotPersistence<TDbContext>>();
        }

        public static IEventFlowOptions UseEntityFrameworkReadModel<TReadModel, TDbContext>(
            this IEventFlowOptions eventFlowOptions)
            where TDbContext : DbContext
            where TReadModel : class, IReadModel, new()
        {
            return eventFlowOptions
                .RegisterServices(f =>
                {
                    f.Register<IEntityFrameworkReadModelStore<TReadModel>,
                        EntityFrameworkReadModelStore<TReadModel, TDbContext>>();
                    f.Register<IApplyQueryableConfiguration<TReadModel>>(_ => 
                        new EntityFrameworkReadModelConfiguration<TReadModel>(), Lifetime.Singleton);
                    f.Register<IReadModelStore<TReadModel>>(r =>
                        r.Resolver.Resolve<IEntityFrameworkReadModelStore<TReadModel>>());
                })
                .UseReadStoreFor<IEntityFrameworkReadModelStore<TReadModel>, TReadModel>();
        }

        /// <summary>
        /// Configures the read model. Can be used for eager loading of related data by appending .Include(..) / .ThenInclude(..) statements.
        /// </summary>
        /// <typeparam name="TReadModel">The read model's entity type</typeparam>
        /// <typeparam name="TDbContext">The database context type</typeparam>
        /// <param name="eventFlowOptions"><inheritdoc cref="IEventFlowOptions"/></param>
        /// <param name="configure">Function to configure eager loading of related data by appending .Include(..) / .ThenInclude(..) statements.</param>
        /// <remarks>Avoid navigation properties if you create read models for both, the parent entity and the child entity. Otherwise there is a risk of a ordering problem when saving aggregates and updating read modules independently (FOREIGN-KEY constraint)</remarks>
        public static IEventFlowOptions UseEntityFrameworkReadModel<TReadModel, TDbContext>(
            this IEventFlowOptions eventFlowOptions,
            Func<EntityFrameworkReadModelConfiguration<TReadModel>,IApplyQueryableConfiguration<TReadModel>> configure)
            where TDbContext : DbContext
            where TReadModel : class, IReadModel, new()
        {
            return eventFlowOptions
                .RegisterServices(f =>
                {
                    f.Register<IEntityFrameworkReadModelStore<TReadModel>,
                        EntityFrameworkReadModelStore<TReadModel, TDbContext>>();
                    f.Register(_ =>
                    {
                        var readModelConfig = new EntityFrameworkReadModelConfiguration<TReadModel>();
                        return configure != null
                            ? configure(readModelConfig)
                            : readModelConfig;

                    }, Lifetime.Singleton);
                    f.Register<IReadModelStore<TReadModel>>(r =>
                        r.Resolver.Resolve<IEntityFrameworkReadModelStore<TReadModel>>());
                })
                .UseReadStoreFor<IEntityFrameworkReadModelStore<TReadModel>, TReadModel>();
        }

        /// <summary>
        /// Configures the read model. Can be used for eager loading of related data by appending .Include(..) / .ThenInclude(..) statements.
        /// </summary>
        /// <typeparam name="TReadModel">The read model's entity type</typeparam>
        /// <typeparam name="TDbContext">The database context type</typeparam>
        /// <typeparam name="TReadModelLocator">The read model locator type</typeparam>
        /// <param name="eventFlowOptions"><inheritdoc cref="IEventFlowOptions"/></param>
        /// <param name="configure">Function to configure eager loading of related data by appending .Include(..) / .ThenInclude(..) statements.</param>
        /// <remarks>Avoid navigation properties if you create read models for both, the parent entity and the child entity. Otherwise there is a risk of a ordering problem when saving aggregates and updating read modules independently (FOREIGN-KEY constraint)</remarks>
        public static IEventFlowOptions UseEntityFrameworkReadModel<TReadModel, TDbContext, TReadModelLocator>(
            this IEventFlowOptions eventFlowOptions,
            Func<EntityFrameworkReadModelConfiguration<TReadModel>,IApplyQueryableConfiguration<TReadModel>> configure)
            where TDbContext : DbContext
            where TReadModel : class, IReadModel, new()
            where TReadModelLocator : IReadModelLocator
        {
            return eventFlowOptions
                .RegisterServices(f =>
                {
                    f.Register<IEntityFrameworkReadModelStore<TReadModel>,
                        EntityFrameworkReadModelStore<TReadModel, TDbContext>>();
                    f.Register(_ =>
                    {
                        var readModelConfig = new EntityFrameworkReadModelConfiguration<TReadModel>();
                        return configure != null
                            ? configure(readModelConfig)
                            : readModelConfig;
                    }, Lifetime.Singleton);
                    f.Register<IReadModelStore<TReadModel>>(r =>
                        r.Resolver.Resolve<IEntityFrameworkReadModelStore<TReadModel>>());
                })
                .UseReadStoreFor<IEntityFrameworkReadModelStore<TReadModel>, TReadModel, TReadModelLocator>();
        }

        public static IEventFlowOptions UseEntityFrameworkReadModel<TReadModel, TDbContext, TReadModelLocator>(
            this IEventFlowOptions eventFlowOptions)
            where TDbContext : DbContext
            where TReadModel : class, IReadModel, new()
            where TReadModelLocator : IReadModelLocator
        {
            return eventFlowOptions
                .RegisterServices(f =>
                {
                    f.Register<IEntityFrameworkReadModelStore<TReadModel>,
                        EntityFrameworkReadModelStore<TReadModel, TDbContext>>();
                    f.Register<IApplyQueryableConfiguration<TReadModel>>(_ => 
                        new EntityFrameworkReadModelConfiguration<TReadModel>(), Lifetime.Singleton);
                    f.Register<IReadModelStore<TReadModel>>(r =>
                        r.Resolver.Resolve<IEntityFrameworkReadModelStore<TReadModel>>());
                })
                .UseReadStoreFor<IEntityFrameworkReadModelStore<TReadModel>, TReadModel, TReadModelLocator>();
        }

        public static IEventFlowOptions AddDbContextProvider<TDbContext, TContextProvider>(
            this IEventFlowOptions eventFlowOptions,
            Lifetime lifetime = Lifetime.AlwaysUnique)
            where TContextProvider : class, IDbContextProvider<TDbContext>
            where TDbContext : DbContext
        {
            return eventFlowOptions.RegisterServices(s =>
                s.Register<IDbContextProvider<TDbContext>, TContextProvider>(lifetime));
        }
    }
}