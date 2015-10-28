// The MIT License (MIT)
// 
// Copyright (c) 2015 Rasmus Mikkelsen
// Copyright (c) 2015 eBay Software Foundation
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
// 
using System;
using Elasticsearch.Net.ConnectionPool;
using EventFlow.Configuration;
using EventFlow.Extensions;
using Nest;

namespace EventFlow.ReadStores.Elasticsearch.Extensions
{
    public static class EventFlowOptionsExtensions
    {
        public static IEventFlowOptions ConfigureElasticsearch(
            this IEventFlowOptions eventFlowOptions,
            params Uri[] uris)
        {
            var connectionSettings = new ConnectionSettings(new StaticConnectionPool(uris))
                .ThrowOnElasticsearchServerExceptions()
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
                    sr.Register(f => elasticClientFactory());
                    sr.Register<IReadModelDescriptionProvider, ReadModelDescriptionProvider>(Lifetime.Singleton, true);
                });
        }

        public static IEventFlowOptions UseElasticsearchReadModel<TReadModel>(
            this IEventFlowOptions eventFlowOptions)
            where TReadModel : class, IReadModel, new()
        {
            return eventFlowOptions
                .RegisterServices(f =>
                    {
                        f.Register<IElasticsearchReadModelStore<TReadModel>, ElasticsearchReadModelStore<TReadModel>>();
                        f.Register<IReadModelStore<TReadModel>>(r => r.Resolver.Resolve<IElasticsearchReadModelStore<TReadModel>>());
                    })
                .UseReadStoreFor<IElasticsearchReadModelStore<TReadModel>, TReadModel>();
        }
    }
}