﻿// The MIT License (MIT)
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Exceptions;
using EventFlow.Extensions;
using EventFlow.Subscribers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using EventFlow.TestHelpers.Extensions;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventFlow.TestHelpers.Suites
{
    public abstract class TestSuiteForEventStore : IntegrationTest
    {
        private readonly List<IDomainEvent> _publishedDomainEvents = new List<IDomainEvent>();
        protected IReadOnlyCollection<IDomainEvent> PublishedDomainEvents => _publishedDomainEvents;

        [Test]
        public async Task NewAggregateCanBeLoaded()
        {
            // Act
            var testAggregate = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(ThingyId.New).ConfigureAwait(false);

            // Assert
            testAggregate.Should().NotBeNull();
            testAggregate.IsNew.Should().BeTrue();
        }

        [Test]
        public async Task EventsCanBeStored()
        {
            // Arrange
            var id = ThingyId.New;
            var testAggregate = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(id).ConfigureAwait(false);
            testAggregate.Ping(PingId.New);

            // Act
            var domainEvents = await testAggregate.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);

            // Assert
            domainEvents.Count.Should().Be(1);
            var pingEvent = domainEvents.Single() as IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>;
            pingEvent.Should().NotBeNull();
            pingEvent.AggregateIdentity.Should().Be(id);
            pingEvent.AggregateSequenceNumber.Should().Be(1);
            pingEvent.AggregateType.Should().Be(typeof (ThingyAggregate));
            pingEvent.EventType.Should().Be(typeof (ThingyPingEvent));
            pingEvent.Timestamp.Should().NotBe(default(DateTimeOffset));
            pingEvent.Metadata.Count.Should().BeGreaterThan(0);
            pingEvent.Metadata.SourceId.IsNone().Should().BeFalse();
        }

        [Test]
        public async Task AggregatesCanBeLoaded()
        {
            // Arrange
            var id = ThingyId.New;
            var testAggregate = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(id).ConfigureAwait(false);
            testAggregate.Ping(PingId.New);
            await testAggregate.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);

            // Act
            var loadedTestAggregate = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(id).ConfigureAwait(false);

            // Assert
            loadedTestAggregate.Should().NotBeNull();
            loadedTestAggregate.IsNew.Should().BeFalse();
            loadedTestAggregate.Version.Should().Be(1);
            loadedTestAggregate.PingsReceived.Count.Should().Be(1);
        }

        [Test]
        public async Task AggregateEventStreamsAreSeperate()
        {
            // Arrange
            var id1 = ThingyId.New;
            var id2 = ThingyId.New;
            var aggregate1 = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(id1).ConfigureAwait(false);
            var aggregate2 = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(id2).ConfigureAwait(false);
            aggregate1.Ping(PingId.New);
            aggregate2.Ping(PingId.New);
            aggregate2.Ping(PingId.New);

            // Act
            await aggregate1.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);
            await aggregate2.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);
            aggregate1 = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(id1, CancellationToken.None).ConfigureAwait(false);
            aggregate2 = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(id2, CancellationToken.None).ConfigureAwait(false);

            // Assert
            aggregate1.Version.Should().Be(1);
            aggregate2.Version.Should().Be(2);
        }

        [Test]
        public async Task DomainEventCanBeLoaded()
        {
            // Arrange
            var id1 = ThingyId.New;
            var id2 = ThingyId.New;
            var pingId1 = PingId.New;
            var pingId2 = PingId.New;
            var aggregate1 = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(id1).ConfigureAwait(false);
            var aggregate2 = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(id2).ConfigureAwait(false);
            aggregate1.Ping(pingId1);
            aggregate2.Ping(pingId2);
            await aggregate1.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);
            await aggregate2.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);

            // Act
            var domainEvents = await EventStore.LoadAllEventsAsync(GlobalPosition.Start, 200, CancellationToken.None).ConfigureAwait(false);

            // Assert
            domainEvents.DomainEvents.Count.Should().BeGreaterOrEqualTo(2);
        }

        [Test]
        public async Task AggregateCanHaveMultipleCommits()
        {
            // Arrange
            var id = ThingyId.New;

            // Act
            var aggregate = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(id).ConfigureAwait(false);
            aggregate.Ping(PingId.New);
            await aggregate.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);
            aggregate = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(id).ConfigureAwait(false);
            aggregate.Ping(PingId.New);
            await aggregate.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);
            aggregate = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(id).ConfigureAwait(false);

            // Assert
            aggregate.PingsReceived.Count.Should().Be(2);
        }

        [Test]
        public async Task AggregateEventStreamsCanBeDeleted()
        {
            // Arrange
            var id1 = ThingyId.New;
            var id2 = ThingyId.New;
            var aggregate1 = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(id1).ConfigureAwait(false);
            var aggregate2 = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(id2).ConfigureAwait(false);
            aggregate1.Ping(PingId.New);
            aggregate2.Ping(PingId.New);
            aggregate2.Ping(PingId.New);
            await aggregate1.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);
            await aggregate2.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);

            // Act
            await EventStore.DeleteAggregateAsync<ThingyAggregate, ThingyId>(id2, CancellationToken.None).ConfigureAwait(false);

            // Assert
            aggregate1 = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(id1).ConfigureAwait(false);
            aggregate2 = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(id2).ConfigureAwait(false);
            aggregate1.Version.Should().Be(1);
            aggregate2.Version.Should().Be(0);
        }

        [Test]
        public async Task NoEventsEmittedIsOk()
        {
            // Arrange
            var id = ThingyId.New;
            var aggregate = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(id, CancellationToken.None).ConfigureAwait(false);

            // Act
            await aggregate.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);
        }

        [Test]
        public async Task NextPositionIsIdOfNextEvent()
        {
            // Arrange
            var id = ThingyId.New;
            var aggregate = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(id).ConfigureAwait(false);
            aggregate.Ping(PingId.New);
            await aggregate.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);

            // Act
            var domainEvents = await EventStore.LoadAllEventsAsync(GlobalPosition.Start, 10, CancellationToken.None).ConfigureAwait(false);

            // Assert
            domainEvents.NextGlobalPosition.Value.Should().NotBe(string.Empty);
        }

        [Test]
        public async Task LoadingFirstPageShouldLoadCorrectEvents()
        {
            // Arrange
            var id = ThingyId.New;
            var pingIds = new[] {PingId.New, PingId.New, PingId.New};
            var aggregate = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(id, CancellationToken.None).ConfigureAwait(false);
            aggregate.Ping(pingIds[0]);
            aggregate.Ping(pingIds[1]);
            aggregate.Ping(pingIds[2]);
            await aggregate.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);

            // Act
            var domainEvents = await EventStore.LoadAllEventsAsync(GlobalPosition.Start, 200, CancellationToken.None).ConfigureAwait(false);

            // Assert
            domainEvents.DomainEvents.OfType<IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>().Should().Contain(e => e.AggregateEvent.PingId == pingIds[0]);
            domainEvents.DomainEvents.OfType<IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>().Should().Contain(e => e.AggregateEvent.PingId == pingIds[1]);
        }

        [Test]
        public async Task OptimisticConcurrency()
        {
            // Arrange
            var id = ThingyId.New;
            var aggregate1 = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(id, CancellationToken.None).ConfigureAwait(false);
            var aggregate2 = await EventStore.LoadAggregateAsync<ThingyAggregate, ThingyId>(id, CancellationToken.None).ConfigureAwait(false);

            aggregate1.DomainErrorAfterFirst();
            aggregate2.DomainErrorAfterFirst();

            // Act
            await aggregate1.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);
            await ThrowsExceptionAsync<OptimisticConcurrencyException>(() => aggregate2.CommitAsync(EventStore, SourceId.New, CancellationToken.None)).ConfigureAwait(false);
        }

        [Test]
        public async Task PublishedDomainEventsHaveAggregateSequenceNumbers()
        {
            // Arrange
            var id = ThingyId.New;
            var pingIds = Many<PingId>(10);

            // Act
            await CommandBus.PublishAsync(
                new ThingyMultiplePingsCommand(id, pingIds))
                .ConfigureAwait(false);

            // Assert
            PublishedDomainEvents.Count.Should().Be(10);
            PublishedDomainEvents.Select(d => d.AggregateSequenceNumber).ShouldAllBeEquivalentTo(Enumerable.Range(1, 10));
        }

        [Test]
        public async Task PublishedDomainEventsContinueAggregateSequenceNumbers()
        {
            // Arrange
            var id = ThingyId.New;
            var pingIds = Many<PingId>(10);
            await CommandBus.PublishAsync(
                new ThingyMultiplePingsCommand(id, pingIds))
                .ConfigureAwait(false);
            _publishedDomainEvents.Clear();

            // Act
            await CommandBus.PublishAsync(
                new ThingyMultiplePingsCommand(id, pingIds))
                .ConfigureAwait(false);

            // Assert
            PublishedDomainEvents.Count.Should().Be(10);
            PublishedDomainEvents.Select(d => d.AggregateSequenceNumber).ShouldAllBeEquivalentTo(Enumerable.Range(11, 10));
        }

        [SetUp]
        public void TestSuiteForEventStoreSetUp()
        {
            _publishedDomainEvents.Clear();
        }

        protected override IEventFlowOptions Options(IEventFlowOptions eventFlowOptions)
        {
            var subscribeSynchronousToAllMock = new Mock<ISubscribeSynchronousToAll>();

            subscribeSynchronousToAllMock
                .Setup(s => s.HandleAsync(It.IsAny<IReadOnlyCollection<IDomainEvent>>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyCollection<IDomainEvent>, CancellationToken>((d, c) => _publishedDomainEvents.AddRange(d))
                .Returns(Task.FromResult(0));

            return base.Options(eventFlowOptions)
                .RegisterServices(sr => sr.Register(r => subscribeSynchronousToAllMock.Object, Lifetime.Singleton));
        }

        private static async Task ThrowsExceptionAsync<TException>(Func<Task> action)
            where TException : Exception
        {
            var wasCorrectException = false;

            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                wasCorrectException = e.GetType() == typeof (TException);
                if (!wasCorrectException)
                {
                    throw;
                }
            }

            wasCorrectException.Should().BeTrue("Action was expected to throw exception {0}", typeof(TException).PrettyPrint());
        }
    }
}
