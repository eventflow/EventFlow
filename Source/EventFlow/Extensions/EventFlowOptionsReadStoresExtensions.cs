// The MIT License (MIT)
// 
// Copyright (c) 2015-2020 Rasmus Mikkelsen
// Copyright (c) 2015-2020 eBay Software Foundation
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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Queries;
using EventFlow.ReadStores;
using EventFlow.ReadStores.InMemory;
using EventFlow.ReadStores.InMemory.Queries;

namespace EventFlow.Extensions
{
    public static class EventFlowOptionsReadStoresExtensions
    {
        private static readonly MethodInfo UseSingleAggregateRestoreMethod =
            typeof(EventFlowOptionsReadStoresExtensions)
                .GetTypeInfo()
                .GetMethods()
                .Single(m => m.Name == nameof(UseReadStoreFor) && m.GetGenericArguments().Length == 4);

        public static IEventFlowOptions UseReadStoreFor<TReadStore, TReadModel>(
            this IEventFlowOptions eventFlowOptions)
            where TReadStore : class, IReadModelStore<TReadModel>
            where TReadModel : class, IReadModel
        {
            (Type aggregateType, Type idType) = GetSingleAggregateTypes<TReadModel>();

            return (IEventFlowOptions)
                UseSingleAggregateRestoreMethod
                    .MakeGenericMethod(aggregateType, idType, typeof(TReadStore), typeof(TReadModel))
                    .Invoke(null, new object[] {eventFlowOptions});
        }

        [Obsolete("Use the simpler method UseReadStoreFor<TReadStore, TReadModel> instead.")]
        public static IEventFlowOptions UseReadStoreFor<TAggregate, TIdentity, TReadStore, TReadModel>(
            this IEventFlowOptions eventFlowOptions)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TReadStore : class, IReadModelStore<TReadModel>
            where TReadModel : class, IReadModel
        {
            return eventFlowOptions.RegisterServices(f =>
            {
                f.Register<IReadStoreManager,
                    SingleAggregateReadStoreManager<TAggregate, TIdentity, TReadStore, TReadModel>>();
                f.Register<IQueryHandler<ReadModelByIdQuery<TReadModel>, TReadModel>,
                    ReadModelByIdQueryHandler<TReadStore, TReadModel>>();
            });
        }

        public static IEventFlowOptions UseReadStoreFor<TReadStore, TReadModel, TReadModelLocator>(
            this IEventFlowOptions eventFlowOptions)
            where TReadStore : class, IReadModelStore<TReadModel>
            where TReadModel : class, IReadModel
            where TReadModelLocator : IReadModelLocator
        {
            return eventFlowOptions.RegisterServices(f =>
            {
                f.Register<IReadStoreManager,
                    MultipleAggregateReadStoreManager<TReadStore, TReadModel, TReadModelLocator>>();
                f.Register<IQueryHandler<ReadModelByIdQuery<TReadModel>, TReadModel>,
                    ReadModelByIdQueryHandler<TReadStore, TReadModel>>();
            });
        }

        public static IEventFlowOptions UseInMemoryReadStoreFor<TReadModel>(
            this IEventFlowOptions eventFlowOptions)
            where TReadModel : class, IReadModel
        {
            return eventFlowOptions
                .RegisterServices(RegisterInMemoryReadStore<TReadModel>)
                .UseReadStoreFor<IInMemoryReadStore<TReadModel>, TReadModel>();
        }

        [Obsolete("Use the simpler method UseInMemoryReadStoreFor<TReadModel> instead.")]
        public static IEventFlowOptions UseInMemoryReadStoreFor<TAggregate, TIdentity, TReadModel>(
            this IEventFlowOptions eventFlowOptions)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TReadModel : class, IReadModel
        {
            return eventFlowOptions
                .RegisterServices(RegisterInMemoryReadStore<TReadModel>)
                .UseReadStoreFor<TAggregate, TIdentity, IInMemoryReadStore<TReadModel>, TReadModel>();
        }

        public static IEventFlowOptions UseInMemoryReadStoreFor<TReadModel, TReadModelLocator>(
            this IEventFlowOptions eventFlowOptions)
            where TReadModel : class, IReadModel
            where TReadModelLocator : IReadModelLocator
        {
            return eventFlowOptions
                .RegisterServices(RegisterInMemoryReadStore<TReadModel>)
                .UseReadStoreFor<IInMemoryReadStore<TReadModel>, TReadModel, TReadModelLocator>();
        }

        private static void RegisterInMemoryReadStore<TReadModel>(
            IServiceRegistration serviceRegistration)
            where TReadModel : class, IReadModel
        {
            serviceRegistration.Register<IInMemoryReadStore<TReadModel>, InMemoryReadStore<TReadModel>>(
                Lifetime.Singleton);
            serviceRegistration.Register<IReadModelStore<TReadModel>>(r =>
                r.Resolver.Resolve<IInMemoryReadStore<TReadModel>>());
            serviceRegistration
                .Register<IQueryHandler<InMemoryQuery<TReadModel>, IReadOnlyCollection<TReadModel>>,
                    InMemoryQueryHandler<TReadModel>>();
        }

        private static (Type aggregateType, Type idType) GetSingleAggregateTypes<TReadModel>()
            where TReadModel : class, IReadModel
        {
            Type readModelInterface = typeof(IAmReadModelFor<,,>);
            Type asyncReadModelInterface = typeof(IAmAsyncReadModelFor<,,>);

            bool IsReadModelInterface(Type type)
            {
                TypeInfo info = type.GetTypeInfo();
                if (!info.IsGenericType) return false;
                Type definition = info.GetGenericTypeDefinition();
                return definition == readModelInterface || definition == asyncReadModelInterface;
            }

            Type readModelType = typeof(TReadModel);
            var results = readModelType
                .GetTypeInfo()
                .GetInterfaces()
                .Where(IsReadModelInterface)
                .GroupBy(i => new
                {
                    AggregateType = i.GenericTypeArguments[0],
                    IdType = i.GenericTypeArguments[1]
                })
                .ToList();

            if (!results.Any())
            {
                var message = $"You are trying to register ReadModel type {typeof(TReadModel).PrettyPrint()} " +
                              "which doesn't subscribe to any events. Implement " +
                              "the IAmReadModelFor<,,> or IAmAsyncReadModelFor<,,> interfaces.";

                throw new InvalidOperationException(message);
            }

            if (results.Count > 1)
            {
                var message = $"You are trying to register ReadModel type {typeof(TReadModel).PrettyPrint()} " +
                              "which subscribes to events from different aggregates. " +
                              "Use a ReadModelLocator, like this: " +
                              $"options.UseSomeReadStoreFor<{typeof(TReadModel)},MyReadModelLocator>";

                throw new InvalidOperationException(message);
            }

            var result = results.Single();
            return (result.Key.AggregateType, result.Key.IdType);
        }
    }
}
