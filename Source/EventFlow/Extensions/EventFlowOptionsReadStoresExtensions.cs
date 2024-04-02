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
using System.Linq;
using System.Reflection;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Queries;
using EventFlow.ReadStores;
using EventFlow.ReadStores.InMemory;
using EventFlow.ReadStores.InMemory.Queries;
using Microsoft.Extensions.DependencyInjection;

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
            eventFlowOptions.ServiceCollection
                .AddTransient<IReadStoreManager, SingleAggregateReadStoreManager<TAggregate, TIdentity, TReadStore, TReadModel>>()
                .AddTransient<IQueryHandler<ReadModelByIdQuery<TReadModel>, TReadModel>, ReadModelByIdQueryHandler<TReadStore, TReadModel>>();
            return eventFlowOptions;
        }

        public static IEventFlowOptions UseReadStoreFor<TReadStore, TReadModel, TReadModelLocator>(
            this IEventFlowOptions eventFlowOptions)
            where TReadStore : class, IReadModelStore<TReadModel>
            where TReadModel : class, IReadModel
            where TReadModelLocator : IReadModelLocator
        {
            eventFlowOptions.ServiceCollection
                .AddTransient<IReadStoreManager, MultipleAggregateReadStoreManager<TReadStore, TReadModel, TReadModelLocator>>()
                .AddTransient<IQueryHandler<ReadModelByIdQuery<TReadModel>, TReadModel>, ReadModelByIdQueryHandler<TReadStore, TReadModel>>();
            return eventFlowOptions;
        }

        public static IEventFlowOptions UseInMemoryReadStoreFor<TReadModel>(
            this IEventFlowOptions eventFlowOptions)
            where TReadModel : class, IReadModel
        {
            RegisterInMemoryReadStore<TReadModel>(eventFlowOptions.ServiceCollection);
            eventFlowOptions.UseReadStoreFor<IInMemoryReadStore<TReadModel>, TReadModel>();
            return eventFlowOptions;
        }

        public static IEventFlowOptions UseInMemoryReadStoreFor<TReadModel, TReadModelLocator>(
            this IEventFlowOptions eventFlowOptions)
            where TReadModel : class, IReadModel
            where TReadModelLocator : IReadModelLocator
        {
            RegisterInMemoryReadStore<TReadModel>(eventFlowOptions.ServiceCollection);
            eventFlowOptions
                .UseReadStoreFor<IInMemoryReadStore<TReadModel>, TReadModel, TReadModelLocator>();
            return eventFlowOptions;
        }

        private static void RegisterInMemoryReadStore<TReadModel>(
            IServiceCollection serviceCollection)
            where TReadModel : class, IReadModel
        {
            serviceCollection.AddSingleton<IInMemoryReadStore<TReadModel>, InMemoryReadStore<TReadModel>>();
            serviceCollection.AddTransient<IReadModelStore<TReadModel>>(r => r.GetRequiredService<IInMemoryReadStore<TReadModel>>());
            serviceCollection.AddTransient<IQueryHandler<InMemoryQuery<TReadModel>, IReadOnlyCollection<TReadModel>>, InMemoryQueryHandler<TReadModel>>();
        }

        private static (Type aggregateType, Type idType) GetSingleAggregateTypes<TReadModel>()
            where TReadModel : class, IReadModel
        {
            var readModelInterface = typeof(IAmReadModelFor<,,>);
            var asyncReadModelInterface = typeof(IAmReadModelFor<,,>);

            bool IsReadModelInterface(Type type)
            {
                TypeInfo info = type.GetTypeInfo();
                if (!info.IsGenericType) return false;
                Type definition = info.GetGenericTypeDefinition();
                return definition == readModelInterface || definition == asyncReadModelInterface;
            }

            var readModelType = typeof(TReadModel);
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
