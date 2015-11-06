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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.ReadStores.Elasticsearch.Extensions;
using EventFlow.TestHelpers.Suites;
using Nest;
using NUnit.Framework;

namespace EventFlow.ReadStores.Elasticsearch.Tests.Integration
{
    public class ElasticsearchReadModelStoreTests : TestSuiteForReadModelStore
    {
        private IElasticClient _elasticClient;
        private IReadModelDescriptionProvider _readModelDescriptionProvider;

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            // Disable SSL certificate validation
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            var url = Environment.GetEnvironmentVariable("ELASTICSEARCH_URL");
            if (string.IsNullOrEmpty(url))
            {
                Assert.Inconclusive("The environment variabel named 'ELASTICSEARCH_URL' isn't set. Set it to e.g. 'http://localhost:9200'");
            }

            var resolver = eventFlowOptions
                .ConfigureElasticsearch(new Uri(url))
                .UseElasticsearchReadModel<ElasticsearchThingyReadModel>()
                .CreateResolver();

            _elasticClient = resolver.Resolve<IElasticClient>();
            _readModelDescriptionProvider = resolver.Resolve<IReadModelDescriptionProvider>();

            return resolver;
        }

        protected override Task PurgeTestAggregateReadModelAsync()
        {
            return ReadModelPopulator.PurgeAsync<ElasticsearchThingyReadModel>(CancellationToken.None);
        }

        protected override Task PopulateTestAggregateReadModelAsync()
        {
            return ReadModelPopulator.PopulateAsync<ElasticsearchThingyReadModel>(CancellationToken.None);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<ElasticsearchThingyReadModel>();
                _elasticClient.DeleteIndex(readModelDescription.IndexName.Value);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}