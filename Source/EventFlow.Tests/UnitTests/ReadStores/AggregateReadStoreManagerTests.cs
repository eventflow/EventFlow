// The MIT License (MIT)
// 
// Copyright (c) 2015-2018 Rasmus Mikkelsen
// Copyright (c) 2015-2018 eBay Software Foundation
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using EventFlow.ReadStores;
using EventFlow.ReadStores.InMemory;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Extensions;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.ReadStores
{
    [Category(Categories.Unit)]
    public class AggregateReadStoreManagerTests : ReadStoreManagerTestSuite<AggregateReadStoreManager<
        ThingyAggregate,
        ThingyId,
        IReadModelStore<TestReadModel>,
        TestReadModel>>
    {
        private Mock<IEventStore> _eventStoreMock;

        [SetUp]
        public void SetUp()
        {
            _eventStoreMock = InjectMock<IEventStore>();
        }

        [Test]
        public async Task EventsAreApplied()
        {
            // Arrange
            var thingyId = A<ThingyId>();
            var emittedEvents = new[]
                {
                    ToDomainEvent(thingyId, A<ThingyPingEvent>(), 3),
                };
            Arrange_ReadModelStore_UpdateAsync(ReadModelEnvelope<TestReadModel>.With(
                thingyId.Value,
                A<TestReadModel>(),
                2));

            // Act
            await Sut.UpdateReadStoresAsync(emittedEvents, CancellationToken.None).ConfigureAwait(false);

            // Assert
            AppliedDomainEvents.Should().HaveCount(emittedEvents.Length);
            AppliedDomainEvents.Should().BeEquivalentTo(emittedEvents);
        }

        [Test]
        public async Task AlreadyAppliedEventsAreNotApplied()
        {
            // Arrange
            var thingyId = A<ThingyId>();
            var emittedEvents = new[]
                {
                    ToDomainEvent(thingyId, A<ThingyPingEvent>(), 3),
                };
            var resultingReadModelUpdates = Arrange_ReadModelStore_UpdateAsync(ReadModelEnvelope<TestReadModel>.With(
                thingyId.Value,
                A<TestReadModel>(),
                3));

            // Act
            await Sut.UpdateReadStoresAsync(emittedEvents, CancellationToken.None).ConfigureAwait(false);

            // Assert
            AppliedDomainEvents.Should().BeEmpty();
            resultingReadModelUpdates.Single().IsModified.Should().BeFalse();
        }

        [Test]
        public async Task OutdatedEventsAreNotApplied()
        {
            // Arrange
            var thingyId = A<ThingyId>();
            var emittedEvents = new[]
                {
                    ToDomainEvent(thingyId, A<ThingyPingEvent>(), 1),
                };
            Arrange_ReadModelStore_UpdateAsync(ReadModelEnvelope<TestReadModel>.With(
                thingyId.Value,
                A<TestReadModel>(),
                3));

            // Act
            await Sut.UpdateReadStoresAsync(emittedEvents, CancellationToken.None);

            // Assert
            AppliedDomainEvents.Should().BeEmpty();
        }

        [Test]
        public async Task StoredEventsAreAppliedIfThereAreMissingEvents()
        {
            // Arrange
            var thingyId = A<ThingyId>();
            var emittedEvents = new[]
                {
                    ToDomainEvent(thingyId, A<ThingyPingEvent>(), 3),
                    ToDomainEvent(thingyId, A<ThingyPingEvent>(), 4),
                };
            var missingEvents = new[]
                {
                    ToDomainEvent(thingyId, A<ThingyPingEvent>(), 2)
                };
            var storedEvents = Enumerable.Empty<IDomainEvent<ThingyAggregate, ThingyId>>()
                .Concat(missingEvents)
                .Concat(emittedEvents)
                .ToArray();
            Arrange_ReadModelStore_UpdateAsync(ReadModelEnvelope<TestReadModel>.With(
                thingyId.Value,
                A<TestReadModel>(),
                1));
            _eventStoreMock.Arrange_LoadEventsAsync(storedEvents);

            // Act
            await Sut.UpdateReadStoresAsync(emittedEvents, CancellationToken.None).ConfigureAwait(false);

            // Assert
            AppliedDomainEvents.Should().HaveCount(storedEvents.Length);
            AppliedDomainEvents.Should().BeEquivalentTo(storedEvents);
        }

        [Test]
        public void ThrowsIfReadModelSubscribesNoEvents()
        {
            Action a = () =>
            {
                var _ = new SingleAggregateReadStoreManager<InMemoryReadStore<ReadModelWithoutEvents>,
                    ReadModelWithoutEvents>(null, null, null, null, null);
            };

            a.Should().Throw<TypeInitializationException>().WithInnerException<Exception>().WithMessage("*does not implement any*");
        }

        [Test]
        public void ThrowsIfReadModelSubscribesSameEventTwice()
        {
            Action a = () =>
            {
                var _ = new SingleAggregateReadStoreManager<InMemoryReadStore<ReadModelWithAmbigiousEvents>,
                    ReadModelWithAmbigiousEvents>(null, null, null, null, null);
            };

            a.Should().Throw<TypeInitializationException>().WithInnerException<Exception>().WithMessage("*implements ambiguous*");
        }

        private class ReadModelWithoutEvents : IReadModel
        {
        }

        private class ReadModelWithAmbigiousEvents : IReadModel,
            IAmReadModelFor<ThingyAggregate, ThingyId, ThingyPingEvent>,
            IAmAsyncReadModelFor<ThingyAggregate, ThingyId, ThingyPingEvent>
        {
            public void Apply(IReadModelContext context, IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent> domainEvent)
            {
            }

            public Task ApplyAsync(IReadModelContext context, IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent> domainEvent,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(true);
            }
        }
    }
}