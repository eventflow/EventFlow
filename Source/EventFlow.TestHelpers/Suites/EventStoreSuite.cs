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
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Exceptions;
using EventFlow.Extensions;
using EventFlow.TestHelpers.Aggregates.Test;
using EventFlow.TestHelpers.Aggregates.Test.Events;
using EventFlow.TestHelpers.Aggregates.Test.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.TestHelpers.Suites
{
    public class EventStoreSuite<TConfiguration> : IntegrationTest<TConfiguration>
        where TConfiguration : IntegrationTestConfiguration, new()
    {
        [Test]
        public async Task NewAggregateCanBeLoaded()
        {
            // Act
            var testAggregate = await EventStore.LoadAggregateAsync<TestAggregate, TestId>(TestId.New, CancellationToken.None).ConfigureAwait(false);

            // Assert
            testAggregate.Should().NotBeNull();
            testAggregate.IsNew.Should().BeTrue();
        }

        [Test]
        public async Task EventsCanBeStored()
        {
            // Arrange
            var id = TestId.New;
            var testAggregate = await EventStore.LoadAggregateAsync<TestAggregate, TestId>(id, CancellationToken.None).ConfigureAwait(false);
            testAggregate.Ping(PingId.New);

            // Act
            var domainEvents = await testAggregate.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);

            // Assert
            domainEvents.Count.Should().Be(1);
            var pingEvent = domainEvents.Single() as IDomainEvent<TestAggregate, TestId, PingEvent>;
            pingEvent.Should().NotBeNull();
            pingEvent.AggregateIdentity.Should().Be(id);
            pingEvent.AggregateSequenceNumber.Should().Be(1);
            pingEvent.AggregateType.Should().Be(typeof (TestAggregate));
            pingEvent.EventType.Should().Be(typeof (PingEvent));
            pingEvent.Timestamp.Should().NotBe(default(DateTimeOffset));
            pingEvent.Metadata.Count.Should().BeGreaterThan(0);
            pingEvent.Metadata.SourceId.IsNone().Should().BeFalse();
        }

        [Test]
        public async Task AggregatesCanBeLoaded()
        {
            // Arrange
            var id = TestId.New;
            var testAggregate = await EventStore.LoadAggregateAsync<TestAggregate, TestId>(id, CancellationToken.None).ConfigureAwait(false);
            testAggregate.Ping(PingId.New);
            await testAggregate.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);

            // Act
            var loadedTestAggregate = await EventStore.LoadAggregateAsync<TestAggregate, TestId>(id, CancellationToken.None).ConfigureAwait(false);

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
            var id1 = TestId.New;
            var id2 = TestId.New;
            var aggregate1 = await EventStore.LoadAggregateAsync<TestAggregate, TestId>(id1, CancellationToken.None).ConfigureAwait(false);
            var aggregate2 = await EventStore.LoadAggregateAsync<TestAggregate, TestId>(id2, CancellationToken.None).ConfigureAwait(false);
            aggregate1.Ping(PingId.New);
            aggregate2.Ping(PingId.New);
            aggregate2.Ping(PingId.New);

            // Act
            await aggregate1.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);
            await aggregate2.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);
            aggregate1 = await EventStore.LoadAggregateAsync<TestAggregate, TestId>(id1, CancellationToken.None).ConfigureAwait(false);
            aggregate2 = await EventStore.LoadAggregateAsync<TestAggregate, TestId>(id2, CancellationToken.None).ConfigureAwait(false);

            // Assert
            aggregate1.Version.Should().Be(1);
            aggregate2.Version.Should().Be(2);
        }

        [Test]
        public async Task DomainEventCanBeLoaded()
        {
            // Arrange
            var id1 = TestId.New;
            var id2 = TestId.New;
            var pingId1 = PingId.New;
            var pingId2 = PingId.New;
            var aggregate1 = await EventStore.LoadAggregateAsync<TestAggregate, TestId>(id1, CancellationToken.None).ConfigureAwait(false);
            var aggregate2 = await EventStore.LoadAggregateAsync<TestAggregate, TestId>(id2, CancellationToken.None).ConfigureAwait(false);
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
        public async Task AggregateEventStreamsCanBeDeleted()
        {
            // Arrange
            var id1 = TestId.New;
            var id2 = TestId.New;
            var aggregate1 = await EventStore.LoadAggregateAsync<TestAggregate, TestId>(id1, CancellationToken.None).ConfigureAwait(false);
            var aggregate2 = await EventStore.LoadAggregateAsync<TestAggregate, TestId>(id2, CancellationToken.None).ConfigureAwait(false);
            aggregate1.Ping(PingId.New);
            aggregate2.Ping(PingId.New);
            aggregate2.Ping(PingId.New);
            await aggregate1.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);
            await aggregate2.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);

            // Act
            await EventStore.DeleteAggregateAsync<TestAggregate, TestId>(id2, CancellationToken.None).ConfigureAwait(false);

            // Assert
            aggregate1 = await EventStore.LoadAggregateAsync<TestAggregate, TestId>(id1, CancellationToken.None).ConfigureAwait(false);
            aggregate2 = await EventStore.LoadAggregateAsync<TestAggregate, TestId>(id2, CancellationToken.None).ConfigureAwait(false);
            aggregate1.Version.Should().Be(1);
            aggregate2.Version.Should().Be(0);
        }

        [Test]
        public async Task NoEventsEmittedIsOk()
        {
            // Arrange
            var id = TestId.New;
            var aggregate = await EventStore.LoadAggregateAsync<TestAggregate, TestId>(id, CancellationToken.None).ConfigureAwait(false);

            // Act
            await aggregate.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);
        }

        [Test]
        public async Task NextPositionIsIdOfNextEvent()
        {
            // Arrange
            var id = TestId.New;
            var aggregate = await EventStore.LoadAggregateAsync<TestAggregate, TestId>(id, CancellationToken.None).ConfigureAwait(false);
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
            var id = TestId.New;
            var pingIds = new[] {PingId.New, PingId.New, PingId.New};
            var aggregate = await EventStore.LoadAggregateAsync<TestAggregate, TestId>(id, CancellationToken.None).ConfigureAwait(false);
            aggregate.Ping(pingIds[0]);
            aggregate.Ping(pingIds[1]);
            aggregate.Ping(pingIds[2]);
            await aggregate.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);

            // Act
            var domainEvents = await EventStore.LoadAllEventsAsync(GlobalPosition.Start, 200, CancellationToken.None).ConfigureAwait(false);

            // Assert
            domainEvents.DomainEvents.Should().Contain(e => ((IDomainEvent<TestAggregate, TestId, PingEvent>)e).AggregateEvent.PingId == pingIds[0]);
            domainEvents.DomainEvents.Should().Contain(e => ((IDomainEvent<TestAggregate, TestId, PingEvent>)e).AggregateEvent.PingId == pingIds[1]);
        }

        [Test]
        public async Task OptimisticConcurrency()
        {
            // Arrange
            var id = TestId.New;
            var aggregate1 = await EventStore.LoadAggregateAsync<TestAggregate, TestId>(id, CancellationToken.None).ConfigureAwait(false);
            var aggregate2 = await EventStore.LoadAggregateAsync<TestAggregate, TestId>(id, CancellationToken.None).ConfigureAwait(false);

            aggregate1.DomainErrorAfterFirst();
            aggregate2.DomainErrorAfterFirst();

            // Act
            await aggregate1.CommitAsync(EventStore, SourceId.New, CancellationToken.None).ConfigureAwait(false);
            await ThrowsExceptionAsync<OptimisticConcurrencyException>(() => aggregate2.CommitAsync(EventStore, SourceId.New, CancellationToken.None)).ConfigureAwait(false);
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
