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
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Extensions;
using EventFlow.MongoDB.Extensions;
using EventFlow.MongoDB.Tests.IntegrationTests.ReadStores.Queries;
using EventFlow.MongoDB.Tests.IntegrationTests.ReadStores.QueryHandlers;
using EventFlow.MongoDB.Tests.IntegrationTests.ReadStores.ReadModels;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Extensions;
using EventFlow.TestHelpers.Suites;
using FluentAssertions;
using Mongo2Go;
using NUnit.Framework;

namespace EventFlow.MongoDB.Tests.IntegrationTests.ReadStores
{
    [Category(Categories.Integration)]
    [TestFixture]
    public class MongoDbReadModelStoreTests : TestSuiteForReadModelStore
    {
        protected override Type ReadModelType { get; } = typeof(MongoDbThingyReadModel);

        private MongoDbRunner _runner;

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            _runner = MongoDbRunner.Start();

            var resolver = eventFlowOptions
                .RegisterServices(sr => { sr.RegisterType(typeof(ThingyMessageLocator)); })
                .ConfigureMongoDb(_runner.ConnectionString, "eventflow")
                .UseMongoDbReadModel<MongoDbThingyReadModel>()
                .UseMongoDbReadModel<MongoDbThingyMessageReadModel, ThingyMessageLocator>()
                .AddQueryHandlers(
                    typeof(MongoDbThingyGetQueryHandler),
                    typeof(MongoDbThingyGetVersionQueryHandler),
                    typeof(MongoDbThingyGetMessagesQueryHandler),
                    typeof(MongoDbThingyGetWithLinqQueryHandler)
                       )
                .CreateResolver();

            return resolver;
        }

        [Test]
        public async Task AsQueryableShouldNotBeEmpty()
        {

            var id = ThingyId.New;

            await PublishPingCommandsAsync(id, 1).ConfigureAwait(false);

            var result = await QueryProcessor.ProcessAsync(new MongoDbThingyGetWithLinqQuery()).ConfigureAwait(false);

            result.ToList().Should().NotBeEmpty();
        }

        [TearDown]
        public void TearDown()
        {
            _runner.Dispose();
        }
    }
}