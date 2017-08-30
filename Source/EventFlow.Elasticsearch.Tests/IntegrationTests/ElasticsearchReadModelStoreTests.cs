// The MIT License (MIT)
// 
// Copyright (c) 2015-2017 Rasmus Mikkelsen
// Copyright (c) 2015-2017 eBay Software Foundation
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
using System.Net;
using EventFlow.Configuration;
using EventFlow.Elasticsearch.Extensions;
using EventFlow.Elasticsearch.ReadStores;
using EventFlow.Elasticsearch.Tests.IntegrationTests.QueryHandlers;
using EventFlow.Elasticsearch.Tests.IntegrationTests.ReadModels;
using EventFlow.Elasticsearch.ValueObjects;
using EventFlow.Extensions;
using EventFlow.ReadStores;
using EventFlow.TestHelpers;
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
        private ElasticsearchRunner.ElasticsearchInstance _elasticsearchInstance;
        private string _indexName;

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            _elasticsearchInstance = ElasticsearchRunner.StartAsync().Result;
        }

        [OneTimeTearDown]
        public void FixtureTearDown()
        {
            _elasticsearchInstance.DisposeSafe("Failed to close Elasticsearch down");
        }

        public class TestReadModelDescriptionProvider : IReadModelDescriptionProvider
        {
            private readonly string _indexName;

            public TestReadModelDescriptionProvider(
                string indexName)
            {
                _indexName = indexName;
            }

            public ReadModelDescription GetReadModelDescription<TReadModel>() where TReadModel : IReadModel
            {
                return new ReadModelDescription(
                    new IndexName(_indexName));
            }
        }

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            try
            {
                _indexName = $"eventflow-test-{Guid.NewGuid():D}";

                var testReadModelDescriptionProvider = new TestReadModelDescriptionProvider(_indexName);

                var resolver = eventFlowOptions
                    .RegisterServices(sr =>
                        {
                            sr.RegisterType(typeof(ThingyMessageLocator));
                            sr.Register<IReadModelDescriptionProvider>(c => testReadModelDescriptionProvider);
                        })
                    .ConfigureElasticsearch(_elasticsearchInstance.Uri)
                    .UseElasticsearchReadModel<ElasticsearchThingyReadModel>()
                    .UseElasticsearchReadModel<ElasticsearchThingyMessageReadModel, ThingyMessageLocator>()
                    .AddQueryHandlers(
                        typeof(ElasticsearchThingyGetQueryHandler),
                        typeof(ElasticsearchThingyGetVersionQueryHandler),
                        typeof(ElasticsearchThingyGetMessagesQueryHandler))
                    .CreateResolver();

                _elasticClient = resolver.Resolve<IElasticClient>();

                _elasticClient.CreateIndex(_indexName, c => c
                    .Settings(s => s
                        .NumberOfShards(1)
                        .NumberOfReplicas(0))
                    .Mappings(m => m
                        .Map<ElasticsearchThingyMessageReadModel>(d => d
                            .AutoMap())));

                _elasticsearchInstance.WaitForGreenStateAsync().Wait(TimeSpan.FromMinutes(1));

                return resolver;
            }
            catch
            {
                _elasticsearchInstance.DisposeSafe("Failed to dispose ES instance");
                throw;
            }
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                Console.WriteLine($"Deleting test index '{_indexName}'");
                _elasticClient.DeleteIndex(
                    _indexName,
                    r => r.RequestConfiguration(c => c.AllowedStatusCodes((int)HttpStatusCode.NotFound)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}