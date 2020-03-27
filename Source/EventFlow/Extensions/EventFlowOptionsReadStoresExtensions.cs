// The MIT License (MIT)
// 
// Copyright (c) 2015-2018 Rasmus Mikkelsen
// Copyright (c) 2015-2018 eBay Software Foundation
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


using System.Collections.Generic;
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
        public static IEventFlowOptions UseReadStoreFor<TReadStore, TReadModel>(
            this IEventFlowOptions eventFlowOptions)
            where TReadStore : class, IReadModelStore<TReadModel>
            where TReadModel : class, IReadModel
        {
            return eventFlowOptions.RegisterServices(f =>
                {
                    f.Register<IReadStoreManager, SingleAggregateReadStoreManager<TReadStore, TReadModel>>();
                    f.Register<IQueryHandler<ReadModelByIdQuery<TReadModel>,ReadModelResult<TReadModel>>, ReadModelByIdQueryHandler<TReadStore, TReadModel>>();
                });
        }

        public static IEventFlowOptions UseReadStoreFor<TAggregate, TIdentity, TReadStore, TReadModel>(
            this IEventFlowOptions eventFlowOptions)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TReadStore : class, IReadModelStore<TReadModel>
            where TReadModel : class, IReadModel
        {
            return eventFlowOptions.RegisterServices(f =>
                {
                    f.Register<IReadStoreManager, AggregateReadStoreManager<TAggregate, TIdentity, TReadStore, TReadModel>>();
                    f.Register<IQueryHandler<ReadModelByIdQuery<TReadModel>, ReadModelResult<TReadModel>>, ReadModelByIdQueryHandler<TReadStore, TReadModel>>();
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
                    f.Register<IReadStoreManager, MultipleAggregateReadStoreManager<TReadStore, TReadModel, TReadModelLocator>>();
                    f.Register<IQueryHandler<ReadModelByIdQuery<TReadModel>, ReadModelResult<TReadModel>>, ReadModelByIdQueryHandler<TReadStore, TReadModel>>();
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
            serviceRegistration.Register<IInMemoryReadStore<TReadModel>, InMemoryReadStore<TReadModel>>(Lifetime.Singleton);
            serviceRegistration.Register<IReadModelStore<TReadModel>>(r => r.Resolver.Resolve<IInMemoryReadStore<TReadModel>>());
            serviceRegistration.Register<IQueryHandler<InMemoryQuery<TReadModel>, IReadOnlyCollection<TReadModel>>, InMemoryQueryHandler<TReadModel>>();
        }
    }
}
