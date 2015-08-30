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

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores.EventStore.Extensions;
using EventFlow.Extensions;
using EventFlow.MetadataProviders;
using EventFlow.Queries;
using EventFlow.ReadStores;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Test.ReadModels;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace EventFlow.EventStores.EventStore.Tests.IntegrationTests
{
    public class EventStoreEventStoreTestConfiguration : IntegrationTestConfiguration
    {
        private IQueryProcessor _queryProcessor;
        private IReadModelPopulator _readModelPopulator;

        public override IRootResolver CreateRootResolver(EventFlowOptions eventFlowOptions)
        {
            var connectionSettings = ConnectionSettings.Create()
                .EnableVerboseLogging()
                .KeepReconnecting()
                .SetDefaultUserCredentials(new UserCredentials("admin", "changeit"))
                .Build();

            var resolver = eventFlowOptions
                .UseInMemoryReadStoreFor<InMemoryTestAggregateReadModel>()
                .AddMetadataProvider<AddGuidMetadataProvider>()
                .UseEventStoreEventStore(new IPEndPoint(IPAddress.Loopback, 1113), connectionSettings)
                .CreateResolver();

            _queryProcessor = resolver.Resolve<IQueryProcessor>();
            _readModelPopulator = resolver.Resolve<IReadModelPopulator>();

            return resolver;
        }

        public override async Task<ITestAggregateReadModel> GetTestAggregateReadModelAsync(IIdentity id)
        {
            return await _queryProcessor.ProcessAsync(new ReadModelByIdQuery<InMemoryTestAggregateReadModel>(id.Value), CancellationToken.None).ConfigureAwait(false);
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
        }
    }
}
