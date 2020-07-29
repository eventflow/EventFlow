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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Sagas;
using EventFlow.Subscribers;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.Sagas;
using EventFlow.TestHelpers.Aggregates.Sagas.Events;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.IntegrationTests.Sagas
{
    [Category(Categories.Integration)]
    public class AggregateSagaTests : IntegrationTest
    {
        private Mock<ISubscribeSynchronousTo<ThingySaga, ThingySagaId, ThingySagaStartedEvent>> _thingySagaStartedSubscriber;

        [Test]
        public async Task InitialSagaStateIsNew()
        {
            // Act
            var thingySaga = await LoadSagaAsync(A<ThingyId>()).ConfigureAwait(false);

            // Assert
            thingySaga.State.Should().Be(SagaState.New);
        }

        [Test]
        public async Task PublishingEventWithoutStartingSagaLeavesItNew()
        {
            // Arrange
            var thingyId = A<ThingyId>();

            // Act
            await PublishPingCommandAsync(thingyId).ConfigureAwait(false);

            // Assert
            var thingySaga = await LoadSagaAsync(thingyId).ConfigureAwait(false);
            thingySaga.State.Should().Be(SagaState.New);
        }

        [Test]
        public async Task PublishingEventWithoutStartingDoesntPublishToMainAggregate()
        {
            // Arrange
            var thingyId = A<ThingyId>();

            // Act
            await PublishPingCommandAsync(thingyId).ConfigureAwait(false);

            // Assert
            var thingyAggregate = await LoadAggregateAsync(thingyId).ConfigureAwait(false);
            thingyAggregate.Messages.Should().BeEmpty();
        }

        [Test]
        public async Task PublishingCompleteEventWithoutStartingSagaLeavesItNew()
        {
            // Arrange
            var thingyId = A<ThingyId>();

            // Act
            await CommandBus.PublishAsync(new ThingyRequestSagaCompleteCommand(thingyId), CancellationToken.None).ConfigureAwait(false);

            // Assert
            var thingySaga = await LoadSagaAsync(thingyId).ConfigureAwait(false);
            thingySaga.State.Should().Be(SagaState.New);
        }

        [Test]
        public async Task PublishingStartTiggerEventStartsSaga()
        {
            // Arrange
            var thingyId = A<ThingyId>();

            // Act
            await CommandBus.PublishAsync(new ThingyRequestSagaStartCommand(thingyId), CancellationToken.None).ConfigureAwait(false);

            // Assert
            var thingySaga = await LoadSagaAsync(thingyId).ConfigureAwait(false);
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

            // Assert
            var thingySaga = await LoadSagaAsync(thingyId).ConfigureAwait(false);
            thingySaga.State.Should().Be(SagaState.Completed);
        }

        [Test]
        public async Task AggregateSagaEventsArePublishedToSubscribers()
        {
            // Arrange
            var thingyId = A<ThingyId>();

            // Act
            await CommandBus.PublishAsync(new ThingyRequestSagaStartCommand(thingyId), CancellationToken.None).ConfigureAwait(false);

            // Assert
            _thingySagaStartedSubscriber.Verify(
                s => s.HandleAsync(It.IsAny<IDomainEvent<ThingySaga, ThingySagaId, ThingySagaStartedEvent>>(), It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [Test]
        public async Task PublishingStartAndCompleteWithPingsResultInCorrectMessages()
        {
            // Arrange
            var thingyId = A<ThingyId>();

            // Act
            var pingsWithNewSaga = await PublishPingCommandsAsync(thingyId, 3).ConfigureAwait(false);
            await CommandBus.PublishAsync(new ThingyRequestSagaStartCommand(thingyId), CancellationToken.None).ConfigureAwait(false);
            var pingsWithRunningSaga = await PublishPingCommandsAsync(thingyId, 3).ConfigureAwait(false);
            await CommandBus.PublishAsync(new ThingyRequestSagaCompleteCommand(thingyId), CancellationToken.None).ConfigureAwait(false);
            var pingsWithCompletedSaga = await PublishPingCommandsAsync(thingyId, 3).ConfigureAwait(false);

            // Assert - saga
            var thingySaga = await LoadSagaAsync(thingyId).ConfigureAwait(false);
            thingySaga.State.Should().Be(SagaState.Completed);
            thingySaga.PingIdsSinceStarted.Should().BeEquivalentTo(pingsWithRunningSaga);

            // Assert - aggregate
            var thingyAggregate = await LoadAggregateAsync(thingyId).ConfigureAwait(false);
            thingyAggregate.PingsReceived.Should().BeEquivalentTo(
                pingsWithNewSaga.Concat(pingsWithRunningSaga).Concat(pingsWithCompletedSaga));
            var receivedSagaPingIds = thingyAggregate.Messages
                .Select(m => PingId.With(m.Message))
                .ToList();
            receivedSagaPingIds.Should().HaveCount(3);
            receivedSagaPingIds.Should().BeEquivalentTo(pingsWithRunningSaga);
        }

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            _thingySagaStartedSubscriber = new Mock<ISubscribeSynchronousTo<ThingySaga, ThingySagaId, ThingySagaStartedEvent>>();
            _thingySagaStartedSubscriber
                .Setup(s => s.HandleAsync(It.IsAny<IDomainEvent<ThingySaga, ThingySagaId, ThingySagaStartedEvent>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(0));

            return eventFlowOptions
                .RegisterServices(sr => sr.Register(_ => _thingySagaStartedSubscriber.Object))
                .CreateResolver();
        }
    }
}