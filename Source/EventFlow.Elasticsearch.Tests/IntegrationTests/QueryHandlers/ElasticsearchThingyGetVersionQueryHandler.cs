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

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Elasticsearch.ReadStores;
using EventFlow.Elasticsearch.Tests.IntegrationTests.ReadModels;
using EventFlow.Queries;
using EventFlow.TestHelpers.Aggregates.Queries;
using Nest;

namespace EventFlow.Elasticsearch.Tests.IntegrationTests.QueryHandlers
{
    public class ElasticsearchThingyGetVersionQueryHandler : IQueryHandler<ThingyGetVersionQuery, long?>
    {
        private readonly IElasticClient _elasticClient;
        private readonly IReadModelDescriptionProvider _readModelDescriptionProvider;

        public ElasticsearchThingyGetVersionQueryHandler(
            IElasticClient elasticClient,
            IReadModelDescriptionProvider readModelDescriptionProvider)
        {
            _elasticClient = elasticClient;
            _readModelDescriptionProvider = readModelDescriptionProvider;
        }

        public async Task<long?> ExecuteQueryAsync(ThingyGetVersionQuery query, CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<ElasticsearchThingyReadModel>();
            var getResponse = await _elasticClient.GetAsync<ElasticsearchThingyReadModel>(
                query.ThingyId.Value,
                d => d
                    .RequestConfiguration(c => c
                        .AllowedStatusCodes((int)HttpStatusCode.NotFound))
                        .Index(readModelDescription.IndexName.Value), 
                            cancellationToken)
                .ConfigureAwait(false);

            return getResponse != null && getResponse.Found
                ? getResponse.Version
                : null as long?;
        }
    }
}