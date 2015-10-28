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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores.Files;
using EventFlow.Extensions;
using EventFlow.Queries;
using EventFlow.ReadStores;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Test.ReadModels;
using EventFlow.TestHelpers.Suites;

namespace EventFlow.Tests.IntegrationTests.EventStores
{
    public class FilesEventStoreTests : EventStoreSuite<FilesEventStoreTests.FilesConfiguration>
    {
        public class FilesConfiguration : IntegrationTestConfiguration
        {
            private IFilesEventStoreConfiguration _configuration;
            private IReadModelPopulator _readModelPopulator;
            private IQueryProcessor _queryProcessor;

            public override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
            {
                var storePath = Path.Combine(
                    Path.GetTempPath(),
                    Guid.NewGuid().ToString());
                Directory.CreateDirectory(storePath);

                var resolver = eventFlowOptions
                    .UseInMemoryReadStoreFor<InMemoryTestAggregateReadModel>()
                    .UseFilesEventStore(FilesEventStoreConfiguration.Create(storePath))
                    .CreateResolver();

                _configuration = resolver.Resolve<IFilesEventStoreConfiguration>();
                _readModelPopulator = resolver.Resolve<IReadModelPopulator>();
                _queryProcessor = resolver.Resolve<IQueryProcessor>();

                return resolver;
            }

            public override async Task<ITestAggregateReadModel> GetTestAggregateReadModelAsync(IIdentity id)
            {
                return await _queryProcessor.ProcessAsync(
                    new ReadModelByIdQuery<InMemoryTestAggregateReadModel>(id.Value),
                    CancellationToken.None)
                    .ConfigureAwait(false);
            }

            public override Task PurgeTestAggregateReadModelAsync()
            {
                return _readModelPopulator.PurgeAsync<InMemoryTestAggregateReadModel>(CancellationToken.None);
            }

            public override Task PopulateTestAggregateReadModelAsync()
            {
                return _readModelPopulator.PopulateAsync<InMemoryTestAggregateReadModel>(CancellationToken.None);
            }

            public override void TearDown()
            {
                Directory.Delete(_configuration.StorePath, true);
            }
        }
    }
}
