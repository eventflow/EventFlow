// The MIT License (MIT)
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

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.ReadStores.Elasticsearch.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Test.ReadModels;
using Nest;
using NUnit.Framework;

namespace EventFlow.ReadStores.Elasticsearch.Tests.Integration
{
    public class ElasticsearchIntegrationTestConfiguration : IntegrationTestConfiguration
    {
        private IReadModelPopulator _readModelPopulator;
        private IRootResolver _resolver;
        private IElasticClient _elasticClient;
        private IReadModelDescriptionProvider _readModelDescriptionProvider;
        private IElasticsearchReadModelStore<ElasticsearchTestAggregateReadModel> _readModelStore;

        public override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            // Disable SSL certificate validation
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            var url = Environment.GetEnvironmentVariable("ELASTICSEARCH_URL");
            if (string.IsNullOrEmpty(url))
            {
                Assert.Inconclusive("The environment variabel named 'ELASTICSEARCH_URL' isn't set. Set it to e.g. 'http://localhost:9200'");
            }

            _resolver = eventFlowOptions
                .ConfigureElasticsearch(new Uri(url))
                .UseElasticsearchReadModel<ElasticsearchTestAggregateReadModel>()
                .CreateResolver();
            _elasticClient = _resolver.Resolve<IElasticClient>();
            _readModelPopulator = _resolver.Resolve<IReadModelPopulator>();
            _readModelDescriptionProvider = _resolver.Resolve<IReadModelDescriptionProvider>();
            _readModelStore = _resolver.Resolve<IElasticsearchReadModelStore<ElasticsearchTestAggregateReadModel>>();

            return _resolver;
        }

        public override async Task<ITestAggregateReadModel> GetTestAggregateReadModelAsync(IIdentity id)
        {
            var readModelEnvelope = await _readModelStore.GetAsync(id.Value, CancellationToken.None).ConfigureAwait(false);
            return readModelEnvelope.ReadModel;
        }

        public override Task PurgeTestAggregateReadModelAsync()
        {
            return _readModelPopulator.PurgeAsync<ElasticsearchTestAggregateReadModel>(CancellationToken.None);
        }

        public override Task PopulateTestAggregateReadModelAsync()
        {
            return _readModelPopulator.PopulateAsync<ElasticsearchTestAggregateReadModel>(CancellationToken.None);
        }

        public override void TearDown()
        {
            try
            {
                var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<ElasticsearchTestAggregateReadModel>();
                _elasticClient.DeleteIndex(readModelDescription.IndexName.Value);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
