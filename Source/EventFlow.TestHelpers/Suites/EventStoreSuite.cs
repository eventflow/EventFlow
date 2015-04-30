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
using EventFlow.Exceptions;
using EventFlow.TestHelpers.Aggregates.Test;
using EventFlow.TestHelpers.Aggregates.Test.Events;
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
            var testAggregate = await EventStore.LoadAggregateAsync<TestAggregate>(TestId.New, CancellationToken.None).ConfigureAwait(false);

            // Assert
            testAggregate.Should().NotBeNull();
            testAggregate.IsNew.Should().BeTrue();
        }

        [Test]
        public async Task EventsCanBeStored()
        {
            // Arrange
            var id = TestId.New;
            var testAggregate = await EventStore.LoadAggregateAsync<TestAggregate>(id, CancellationToken.None).ConfigureAwait(false);
            testAggregate.Ping();

            // Act
            var domainEvents = await testAggregate.CommitAsync(EventStore, CancellationToken.None).ConfigureAwait(false);

            // Assert
            domainEvents.Count.Should().Be(1);
            var pingEvent = domainEvents.Single() as IDomainEvent<PingEvent>;
            pingEvent.Should().NotBeNull();
            pingEvent.AggregateId.Should().Be(id.Value);
            pingEvent.AggregateSequenceNumber.Should().Be(1);
            pingEvent.AggregateType.Should().Be(typeof (TestAggregate));
            pingEvent.BatchId.Should().NotBe(default(Guid));
            pingEvent.EventType.Should().Be(typeof (PingEvent));
            pingEvent.GlobalSequenceNumber.Should().Be(1);
            pingEvent.Timestamp.Should().NotBe(default(DateTimeOffset));
            pingEvent.Metadata.Count.Should().BeGreaterThan(0);
        }

        [Test]
        public async Task AggregatesCanBeLoaded()
        {
            // Arrange
            var id = TestId.New;
            var testAggregate = await EventStore.LoadAggregateAsync<TestAggregate>(id, CancellationToken.None).ConfigureAwait(false);
            testAggregate.Ping();
            await testAggregate.CommitAsync(EventStore, CancellationToken.None).ConfigureAwait(false);

            // Act
            var loadedTestAggregate = await EventStore.LoadAggregateAsync<TestAggregate>(id, CancellationToken.None).ConfigureAwait(false);

            // Assert
            loadedTestAggregate.Should().NotBeNull();
            loadedTestAggregate.IsNew.Should().BeFalse();
            loadedTestAggregate.Version.Should().Be(1);
            loadedTestAggregate.PingsReceived.Should().Be(1);
        }

        [Test]
        public async Task GlobalSequenceNumberIncrements()
        {
            // Arrange
            var id1 = TestId.New;
            var id2 = TestId.New;
            var aggregate1 = await EventStore.LoadAggregateAsync<TestAggregate>(id1, CancellationToken.None).ConfigureAwait(false);
            var aggregate2 = await EventStore.LoadAggregateAsync<TestAggregate>(id2, CancellationToken.None).ConfigureAwait(false);
            aggregate1.Ping();
            aggregate2.Ping();

            // Act
            await aggregate1.CommitAsync(EventStore, CancellationToken.None).ConfigureAwait(false);
            var domainEvents = await aggregate2.CommitAsync(EventStore, CancellationToken.None).ConfigureAwait(false);

            // Assert
            var pingEvent = domainEvents.SingleOrDefault();
            pingEvent.Should().NotBeNull();
            pingEvent.GlobalSequenceNumber.Should().Be(2);
        }

        [Test]
        public async Task AggregateEventStreamsAreSeperate()
        {
            // Arrange
            var id1 = TestId.New;
            var id2 = TestId.New;
            var aggregate1 = await EventStore.LoadAggregateAsync<TestAggregate>(id1, CancellationToken.None).ConfigureAwait(false);
            var aggregate2 = await EventStore.LoadAggregateAsync<TestAggregate>(id2, CancellationToken.None).ConfigureAwait(false);
            aggregate1.Ping();
            aggregate2.Ping();
            aggregate2.Ping();

            // Act
            await aggregate1.CommitAsync(EventStore, CancellationToken.None).ConfigureAwait(false);
            await aggregate2.CommitAsync(EventStore, CancellationToken.None).ConfigureAwait(false);
            aggregate1 = await EventStore.LoadAggregateAsync<TestAggregate>(id1, CancellationToken.None).ConfigureAwait(false);
            aggregate2 = await EventStore.LoadAggregateAsync<TestAggregate>(id2, CancellationToken.None).ConfigureAwait(false);

            // Assert
            aggregate1.Version.Should().Be(1);
            aggregate2.Version.Should().Be(2);
        }

        [Test]
        public async Task OptimisticConcurrency()
        {
            // Arrange
            var id = TestId.New;
            var aggregate1 = EventStore.LoadAggregate<TestAggregate>(id, CancellationToken.None);
            var aggregate2 = EventStore.LoadAggregate<TestAggregate>(id, CancellationToken.None);

            aggregate1.DomainErrorAfterFirst();
            aggregate2.DomainErrorAfterFirst();

            // Act
            await aggregate1.CommitAsync(EventStore, CancellationToken.None).ConfigureAwait(false);
            Assert.Throws<OptimisticConcurrencyException>(async () => await aggregate2.CommitAsync(EventStore, CancellationToken.None).ConfigureAwait(false));
        }
    }
}
