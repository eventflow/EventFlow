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
using System.Linq;
using System.Net;
using System.Reflection;
using EventFlow.Configuration;
using EventFlow.Elasticsearch.Extensions;
using EventFlow.Elasticsearch.ReadStores;
using EventFlow.Elasticsearch.Tests.IntegrationTests.QueryHandlers;
using EventFlow.Elasticsearch.Tests.IntegrationTests.ReadModels;
using EventFlow.Elasticsearch.ValueObjects;
using EventFlow.Extensions;
using EventFlow.ReadStores;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Suites;
using Nest;
using NUnit.Framework;
using IndexName = EventFlow.Elasticsearch.ValueObjects.IndexName;

namespace EventFlow.Elasticsearch.Tests.IntegrationTests
{
    [Category(Categories.Integration)]
    public class ElasticsearchReadModelStoreTests : TestSuiteForReadModelStore
    {
        protected override Type ReadModelType { get; } = typeof(ElasticsearchThingyReadModel);

        private IElasticClient _elasticClient;

        private readonly List<string> _indexes = new List<string>();

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            var elasticsearchUrl = Environment.GetEnvironmentVariable("ELASTICSEARCH_URL") ?? "http://localhost:9200";

            // Setup connection settings separateley as by default EventFlow uses SniffingConnectionPool
            // which is not working well with elasticserch hosted in local docker
            var connectionSettings = new ConnectionSettings(new Uri(elasticsearchUrl))
                .ThrowExceptions()
                .SniffLifeSpan(TimeSpan.FromMinutes(5))
                .DisablePing();
           
            var resolver = eventFlowOptions
                .RegisterServices(sr => { sr.RegisterType(typeof(ThingyMessageLocator)); })
                .ConfigureElasticsearch(connectionSettings)
                .UseElasticsearchReadModel<ElasticsearchThingyReadModel>()
                .UseElasticsearchReadModel<ElasticsearchThingyMessageReadModel, ThingyMessageLocator>()
                .AddQueryHandlers(
                    typeof(ElasticsearchThingyGetQueryHandler),
                    typeof(ElasticsearchThingyGetVersionQueryHandler),
                    typeof(ElasticsearchThingyGetMessagesQueryHandler))
                .CreateResolver();

            PrepareIndexes(resolver);

            return resolver;
        }

        private void PrepareIndexes(IRootResolver resolver)
        {
            _elasticClient = resolver.Resolve<IElasticClient>();

            var readModelTypes =
                GetLoadableTypes<ElasticsearchTypeAttribute>(typeof(ElasticsearchThingyReadModel).Assembly);

            foreach (var readModelType in readModelTypes)
            {
                var esType = readModelType.GetTypeInfo()
                    .GetCustomAttribute<ElasticsearchTypeAttribute>();

                var aliasResponse = _elasticClient.GetAlias(x => x.Name(esType.Name));

                if (aliasResponse.ApiCall.Success)
                {
                    if (aliasResponse.Indices != null)
                    {
                        foreach (var indice in aliasResponse?.Indices)
                        {
                            _elasticClient.DeleteAlias(indice.Key, esType.Name);

                            _elasticClient.DeleteIndex(indice.Key,
                                d => d.RequestConfiguration(c => c.AllowedStatusCodes((int)HttpStatusCode.NotFound)));
                        }

                        _elasticClient.DeleteIndex(esType.Name,
                            d => d.RequestConfiguration(c => c.AllowedStatusCodes((int)HttpStatusCode.NotFound)));
                    }
                }

                var indexName = GetIndexName(esType.Name);

                _indexes.Add(indexName);

                _elasticClient.CreateIndex(indexName, c => c
                    .Settings(s => s
                        .NumberOfShards(1)
                        .NumberOfReplicas(0))
                    .Aliases(a => a.Alias(esType.Name))
                    .Mappings(m => m
                        .Map(TypeName.Create(readModelType), d => d
                            .AutoMap())));
            }
        }

        private string GetIndexName(string name)
        {
            return $"eventflow-test-{name}-{Guid.NewGuid():D}".ToLowerInvariant();
        }

        private IEnumerable<Type> GetLoadableTypes<T>(params Assembly[] assemblies)
        {
            IEnumerable<Type> availableTypes;

            if (assemblies == null || !assemblies.Any()) throw new ArgumentNullException(nameof(assemblies));
            try
            {
                availableTypes = assemblies.SelectMany(x => x.GetTypes());
            }
            catch (ReflectionTypeLoadException e)
            {
                availableTypes = e.Types.Where(t => t != null);
            }

            foreach (Type type in availableTypes)
            {
                if (type.GetCustomAttributes(typeof(T), true).Length > 0)
                {
                    yield return type;
                }
            }
        }


        [TearDown]
        public void TearDown()
        {
            try
            {
                foreach (var index in _indexes)
                {
                    Console.WriteLine($"Deleting test index '{index}'");
                    _elasticClient.DeleteIndex(
                        index,
                        r => r.RequestConfiguration(c => c.AllowedStatusCodes((int)HttpStatusCode.NotFound)));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}