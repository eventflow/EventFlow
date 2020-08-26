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
using System.Linq;
using Elasticsearch.Net;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Elasticsearch.ReadStores;
using EventFlow.Extensions;
using EventFlow.ReadStores;
using Nest;

namespace EventFlow.Elasticsearch.Extensions
{
    public static class EventFlowOptionsExtensions
    {
        public static IEventFlowOptions ConfigureElasticsearch(
            this IEventFlowOptions eventFlowOptions,
            params string[] uris)
        {
            return eventFlowOptions
                .ConfigureElasticsearch(uris.Select(u => new Uri(u, UriKind.Absolute)).ToArray());
        }

        public static IEventFlowOptions ConfigureElasticsearch(
            this IEventFlowOptions eventFlowOptions,
            params Uri[] uris)
        {
            var connectionSettings = new ConnectionSettings(new SniffingConnectionPool(uris))
                .ThrowExceptions()
                .SniffLifeSpan(TimeSpan.FromMinutes(5))
                .DisablePing();

            return eventFlowOptions
                .ConfigureElasticsearch(connectionSettings);
        }

        public static IEventFlowOptions ConfigureElasticsearch(
            this IEventFlowOptions eventFlowOptions,
            IConnectionSettingsValues connectionSettings)
        {
            var elasticClient = new ElasticClient(connectionSettings);
            return eventFlowOptions.ConfigureElasticsearch(() => elasticClient);
        }

        public static IEventFlowOptions ConfigureElasticsearch(
            this IEventFlowOptions eventFlowOptions,
            Func<IElasticClient> elasticClientFactory)
        {
            return eventFlowOptions.RegisterServices(sr =>
                {
                    sr.Register(f => elasticClientFactory(), Lifetime.Singleton);
                    sr.Register<IReadModelDescriptionProvider, ReadModelDescriptionProvider>(Lifetime.Singleton, true);
                });
        }

        public static IEventFlowOptions UseElasticsearchReadModel<TReadModel>(
            this IEventFlowOptions eventFlowOptions)
            where TReadModel : class, IReadModel
        {
            return eventFlowOptions
                .RegisterServices(RegisterElasticsearchReadStore<TReadModel>)
                .UseReadStoreFor<IElasticsearchReadModelStore<TReadModel>, TReadModel>();
        }

        public static IEventFlowOptions UseElasticsearchReadModel<TReadModel, TReadModelLocator>(
            this IEventFlowOptions eventFlowOptions)
            where TReadModel : class, IReadModel
            where TReadModelLocator : IReadModelLocator
        {
            return eventFlowOptions
                .RegisterServices(RegisterElasticsearchReadStore<TReadModel>)
                .UseReadStoreFor<IElasticsearchReadModelStore<TReadModel>, TReadModel, TReadModelLocator>();
        }

        [Obsolete("Use the simpler method UseElasticsearchReadModel<TReadModel> instead.")]
        public static IEventFlowOptions UseElasticsearchReadModelFor<TAggregate, TIdentity, TReadModel>(
            this IEventFlowOptions eventFlowOptions)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TReadModel : class, IReadModel
        {
            return eventFlowOptions
                .RegisterServices(RegisterElasticsearchReadStore<TReadModel>)
                .UseReadStoreFor<TAggregate, TIdentity, IElasticsearchReadModelStore<TReadModel>, TReadModel>();
        }

        private static void RegisterElasticsearchReadStore<TReadModel>(
            IServiceRegistration serviceRegistration)
            where TReadModel : class, IReadModel
        {
            serviceRegistration.Register<IElasticsearchReadModelStore<TReadModel>, ElasticsearchReadModelStore<TReadModel>>();
            serviceRegistration.Register<IReadModelStore<TReadModel>>(r => r.Resolver.Resolve<IElasticsearchReadModelStore<TReadModel>>());
        }
    }
}