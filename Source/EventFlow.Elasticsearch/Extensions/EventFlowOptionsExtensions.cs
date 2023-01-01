// The MIT License (MIT)
// 
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
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
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Elasticsearch.ReadStores;
using EventFlow.Extensions;
using EventFlow.ReadStores;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventFlow.Elasticsearch.Extensions
{
    public static class EventFlowOptionsExtensions
    {
        public static IEventFlowOptions ConfigureElasticsearch(
            this IEventFlowOptions eventFlowOptions,
            string uri)
        {
            return eventFlowOptions
                .ConfigureElasticsearch( new Uri(uri, UriKind.Absolute));
        }

        public static IEventFlowOptions ConfigureElasticsearch(
            this IEventFlowOptions eventFlowOptions,
            Uri uri)
        {
            
            
            
            var connectionSettings = new ElasticsearchClientSettings(uri)
                .ThrowExceptions()
                .SniffLifeSpan(TimeSpan.FromMinutes(5))
                .DisablePing();

            return eventFlowOptions
                .ConfigureElasticsearch(connectionSettings);
        }

        public static IEventFlowOptions ConfigureElasticsearch(
            this IEventFlowOptions eventFlowOptions,
            ElasticsearchClientSettings connectionSettings)
        {
            var elasticClient = new ElasticsearchClient(connectionSettings);
            return eventFlowOptions.ConfigureElasticsearch(() => elasticClient);
        }

        public static IEventFlowOptions ConfigureElasticsearch(
            this IEventFlowOptions eventFlowOptions,
            Func<ElasticsearchClient> elasticClientFactory)
        {
            return eventFlowOptions.RegisterServices(sr =>
                {
                    sr.TryAddSingleton(f => elasticClientFactory());
                    sr.TryAddSingleton<IReadModelDescriptionProvider, ReadModelDescriptionProvider>();
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
            where TReadModel : class,IReadModel
        {
            return eventFlowOptions
                .RegisterServices(RegisterElasticsearchReadStore<TReadModel>)
                .UseReadStoreFor<TAggregate, TIdentity, IElasticsearchReadModelStore<TReadModel>, TReadModel>();
        }

        private static void RegisterElasticsearchReadStore<TReadModel>(
            IServiceCollection serviceRegistration)
            where TReadModel : class, IReadModel
        {
            serviceRegistration.TryAddTransient<IElasticsearchReadModelStore<TReadModel>, ElasticsearchReadModelStore<TReadModel>>();
            serviceRegistration.TryAddTransient<IReadModelStore<TReadModel>>(r => r.GetService<IElasticsearchReadModelStore<TReadModel>>()!);
        }
    }
}