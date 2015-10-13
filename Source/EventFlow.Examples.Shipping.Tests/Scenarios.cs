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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Examples.Shipping.Domain.Model.LocationModel;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.TestHelpers;
using NUnit.Framework;

namespace EventFlow.Examples.Shipping.Tests
{
    public class Scenarios : Test
    {
        private IRootResolver _resolver;
        private IEventStore _eventStore;

        [SetUp]
        public void SetUp()
        {
            _resolver = EventFlowOptions.New
                .AddDefaults(EventFlowExamplesShipping.Assembly)
                .CreateResolver();
            _eventStore = _resolver.Resolve<IEventStore>();
        }

        [TearDown]
        public void TearDown()
        {
            _resolver.DisposeSafe(new ConsoleLog(), "");
        }

        [Test]
        public async Task Simple()
        {
            await CreateLocationAggregatesAsync().ConfigureAwait(false);
            await CreateVoyageAggregatesAsync().ConfigureAwait(false);

        }

        public Task CreateVoyageAggregatesAsync()
        {
            return Task.WhenAll(Voyages.GetVoyages().Select(CreateVoyageAggregateAsync));
        }

        public Task CreateVoyageAggregateAsync(Voyage voyage)
        {
            return UpdateAsync<VoyageAggregate, VoyageId>(voyage.Id, a => a.Create(voyage.Schedule));
        }

        public Task CreateLocationAggregatesAsync()
        {
            return Task.WhenAll(Locations.GetLocations().Select(CreateLocationAggregateAsync));
        }

        public Task CreateLocationAggregateAsync(Location location)
        {
            return UpdateAsync<LocationAggregate, LocationId>(location.Id, a => a.Create(location.Name));
        }

        private async Task UpdateAsync<TAggregate, TIdentity>(TIdentity id, Action<TAggregate> action)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            var aggregate = await _eventStore.LoadAggregateAsync<TAggregate, TIdentity>(id, CancellationToken.None).ConfigureAwait(false);
            action(aggregate);
            await aggregate.CommitAsync(_eventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);
        }
    }
}