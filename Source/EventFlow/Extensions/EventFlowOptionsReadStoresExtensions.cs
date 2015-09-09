﻿// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
// https://github.com/rasmus/EventFlow
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
using EventFlow.Configuration;
using EventFlow.Queries;
using EventFlow.ReadStores;
using EventFlow.ReadStores.InMemory;
using EventFlow.ReadStores.InMemory.Queries;

namespace EventFlow.Extensions
{
    public static class EventFlowOptionsReadStoresExtensions
    {
        public static EventFlowOptions UseReadStoreFor<TReadStore, TReadModel>(
            this EventFlowOptions eventFlowOptions)
            where TReadStore : class, IReadModelStore<TReadModel>
            where TReadModel : class, IReadModel, new()
        {
            return eventFlowOptions.RegisterServices(f =>
                {
                    f.Register<IReadStoreManager, SingleAggregateReadStoreManager<TReadStore, TReadModel>>();
                    f.Register<IQueryHandler<ReadModelByIdQuery<TReadModel>, TReadModel>, ReadModelByIdQueryHandler<TReadStore, TReadModel>>();
                });
        }

        public static EventFlowOptions UseReadStoreFor<TReadStore, TReadModel, TReadModelLocator>(
            this EventFlowOptions eventFlowOptions)
            where TReadStore : class, IReadModelStore<TReadModel>
            where TReadModel : class, IReadModel, new()
            where TReadModelLocator : IReadModelLocator
        {
            return eventFlowOptions.RegisterServices(f =>
                {
                    f.Register<IReadStoreManager, MultipleAggregateReadStoreManager<TReadStore, TReadModel, TReadModelLocator>>();
                    f.Register<IQueryHandler<ReadModelByIdQuery<TReadModel>, TReadModel>, ReadModelByIdQueryHandler<TReadStore, TReadModel>>();
                });
        }

        public static EventFlowOptions UseInMemoryReadStoreFor<TReadModel>(
            this EventFlowOptions eventFlowOptions)
            where TReadModel : class, IReadModel, new()
        {
            return eventFlowOptions
                .RegisterServices(f =>
                    {
                        f.Register<IInMemoryReadStore<TReadModel>, InMemoryReadStore<TReadModel>>(Lifetime.Singleton);
                        f.Register<IReadModelStore<TReadModel>>(r => r.Resolver.Resolve<IInMemoryReadStore<TReadModel>>());
                        f.Register<IQueryHandler<InMemoryQuery<TReadModel>, IReadOnlyCollection<TReadModel>>, InMemoryQueryHandler<TReadModel>>();
                    })
                .UseReadStoreFor<IInMemoryReadStore<TReadModel>, TReadModel>();
        }

        public static EventFlowOptions UseInMemoryReadStoreFor<TReadModel, TReadModelLocator>(
            this EventFlowOptions eventFlowOptions)
            where TReadModel : class, IReadModel, new()
            where TReadModelLocator : IReadModelLocator
        {
            return eventFlowOptions
                .RegisterServices(f =>
                    {
                        f.Register<IInMemoryReadStore<TReadModel>, InMemoryReadStore<TReadModel>>(Lifetime.Singleton);
                        f.Register<IReadModelStore<TReadModel>>(r => r.Resolver.Resolve<IInMemoryReadStore<TReadModel>>());
                        f.Register<IQueryHandler<InMemoryQuery<TReadModel>, IReadOnlyCollection<TReadModel>>, InMemoryQueryHandler<TReadModel>>();
                    })
                .UseReadStoreFor<IInMemoryReadStore<TReadModel>, TReadModel, TReadModelLocator>();
        }
    }
}
