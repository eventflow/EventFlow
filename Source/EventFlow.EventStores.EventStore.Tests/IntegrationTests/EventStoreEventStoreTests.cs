// The MIT License (MIT)
//
// Copyright (c) 2015-2016 Rasmus Mikkelsen
// Copyright (c) 2015-2016 eBay Software Foundation
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

using EventFlow.Configuration;
using EventFlow.EventStores.EventStore.Extensions;
using EventFlow.Extensions;
using EventFlow.MetadataProviders;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Suites;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using NUnit.Framework;

namespace EventFlow.EventStores.EventStore.Tests.IntegrationTests
{
    [TestFixture]
    [Timeout(30000)]
    [Category(Categories.Integration)]
    public class EventStoreEventStoreTests : TestSuiteForEventStore
    {
        private EventStoreRunner.EventStoreInstance _eventStoreInstance;

        [TestFixtureSetUp]
        public void SetUp()
        {
            _eventStoreInstance = EventStoreRunner.StartAsync().Result; // TODO: Argh, remove .Result
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            _eventStoreInstance.DisposeSafe("EventStore shutdown");
        }

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            var connectionSettings = ConnectionSettings.Create()
                .EnableVerboseLogging()
                .KeepReconnecting()
                .KeepRetrying()
                .SetDefaultUserCredentials(new UserCredentials("admin", "changeit"))
                .Build();

            var resolver = eventFlowOptions
                .AddMetadataProvider<AddGuidMetadataProvider>()
                .UseEventStoreEventStore(_eventStoreInstance.ConnectionStringUri, connectionSettings)
                .CreateResolver();

            return resolver;
        }
    }
}