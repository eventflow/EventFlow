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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.EventStores.EventStore.Extensions;
using EventFlow.Extensions;
using EventFlow.MetadataProviders;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using EventFlow.TestHelpers.Extensions;
using EventFlow.TestHelpers.Suites;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.EventStores.EventStore.Tests.IntegrationTests
{
    [Category(Categories.Integration)]
    [TestFixture]
    public class EventStoreEventStoreTests : TestSuiteForEventStore
    {
        private string _eventStoreHttpUrl;

        protected override IServiceProvider Configure(IEventFlowOptions eventFlowOptions)
        {
            var _eventStoreUri = Environment.GetEnvironmentVariable("EVENTSTORE_URL") ?? "esdb://admin:changeit@localhost:2113?tls=false";
            _eventStoreHttpUrl = Environment.GetEnvironmentVariable("EVENTSTORE_HTTPURL") ?? "http://admin:changeit@localhost:2113";

            eventFlowOptions
                .AddMetadataProvider<AddGuidMetadataProvider>()
                .UseEventStoreEventStore(_eventStoreUri);

            var serviceProvider = base.Configure(eventFlowOptions);
            return serviceProvider;
        }

        private async Task RunScavenge()
        {
            var apiClient = new HttpClient();
            apiClient.BaseAddress = new Uri(_eventStoreHttpUrl);
            await apiClient.PostAsync("/admin/scavenge", null);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        [Test]
        public override async Task LoadAllEventsAsyncFindsEventsAfterLargeGaps()
        {
            // Arrange
            var ids = Enumerable.Range(0, 10)
                .Select(i => ThingyId.New)
                .ToArray();

            foreach (var id in ids)
            {
                var command = new ThingyPingCommand(id, PingId.New);
                await CommandBus.PublishAsync(command);
            }
             
            var removedIds = ids.Skip(1).Take(5);
            var idsWithGap = ids.Where(i => !removedIds.Contains(i));
            foreach (var id in removedIds)
            {
                await EventPersistence.DeleteEventsAsync(id, CancellationToken.None);
            }

            //EventStore has more persistent events that require user to run a scavenge before they are deleted. Recommended to be done daily. 
            await RunScavenge();

            // Act
            var result = await EventStore
                .LoadAllEventsAsync(GlobalPosition.Start, 200, new EventUpgradeContext(), CancellationToken.None)
                ;

            // Assert
            var domainEventIds = result.DomainEvents.Select(d => d.GetIdentity());
            domainEventIds.Should().Contain(idsWithGap);
        }

        [TearDown]
        public void TearDown()
        {
        }
    }
}