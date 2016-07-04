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

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Sagas;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.Sagas;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.IntegrationTests.Sagas
{
    public class AggregateSagaTests : IntegrationTest
    {
        [Test]
        public async Task InitialSagaStateIsNew()
        {
            // Act
            var thingySaga = await GetThingSagaAsync(A<ThingyId>()).ConfigureAwait(false);

            // Assert
            thingySaga.State.Should().Be(SagaState.New);
        }

        [Test]
        public async Task PublishingEventWithoutStartingSagaLeavesItNew()
        {
            // Arrange
            var thingyId = A<ThingyId>();

            // Act
            await PublishPingCommandsAsync(thingyId).ConfigureAwait(false);

            // Assert
            var thingySaga = await GetThingSagaAsync(thingyId).ConfigureAwait(false);
            thingySaga.State.Should().Be(SagaState.New);
        }

        [Test]
        public async Task PublishingStartTiggerEventStartsSaga()
        {
            // Arrange
            var thingyId = A<ThingyId>();

            // Act
            await CommandBus.PublishAsync(new ThingyRequestSagaStartCommand(thingyId), CancellationToken.None).ConfigureAwait(false);

            // Act
            var thingySaga = await GetThingSagaAsync(thingyId).ConfigureAwait(false);
            thingySaga.State.Should().Be(SagaState.Running);
        }

        [Test]
        public async Task PublishingStartAndCompleteTiggerEventsCompletesSaga()
        {
            // Arrange
            var thingyId = A<ThingyId>();

            // Act
            await CommandBus.PublishAsync(new ThingyRequestSagaStartCommand(thingyId), CancellationToken.None).ConfigureAwait(false);
            await CommandBus.PublishAsync(new ThingyRequestSagaCompleteCommand(thingyId), CancellationToken.None).ConfigureAwait(false);

            // Act
            var thingySaga = await GetThingSagaAsync(thingyId).ConfigureAwait(false);
            thingySaga.State.Should().Be(SagaState.Completed);
        }

        private Task<ThingySaga> GetThingSagaAsync(ThingyId thingyId)
        {
            // This is specified in the ThingySagaLocator
            var expectedThingySagaId = new ThingySagaId($"saga-{thingyId.Value}");

            return AggregateStore.LoadAsync<ThingySaga, ThingySagaId>(
                expectedThingySagaId,
                CancellationToken.None);
        }

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions.CreateResolver();
        }
    }
}