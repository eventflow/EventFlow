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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.EventStores;
using EventFlow.ReadStores;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Events;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.ReadStores
{
    [Category(Categories.Unit)]
    public abstract class BaseReadModelTests<TReadModel> : TestsFor<ReadModelPopulator>
        where TReadModel : class, IReadModel
    {
        private const int ReadModelPageSize = 3;

        private Mock<IReadModelStore<TReadModel>> _readModelStoreMock;
        private Mock<IReadStoreManager<TReadModel>> _readStoreManagerMock;
        private Mock<IEventFlowConfiguration> _eventFlowConfigurationMock;
        private Mock<IEventStore> _eventStoreMock;
        private Mock<IResolver> _resolverMock;
        private List<IDomainEvent> _eventStoreData;

        [SetUp]
        public void SetUp()
        {
            _eventStoreMock = InjectMock<IEventStore>();
            _eventStoreData = null;
            _resolverMock = InjectMock<IResolver>();
            _readModelStoreMock = new Mock<IReadModelStore<TReadModel>>();
            _readStoreManagerMock = new Mock<IReadStoreManager<TReadModel>>();
            _eventFlowConfigurationMock = InjectMock<IEventFlowConfiguration>();

            _resolverMock
                .Setup(r => r.Resolve<IEnumerable<IReadStoreManager>>())
                .Returns(new[] { _readStoreManagerMock.Object });
            _resolverMock
                .Setup(r => r.ResolveAll(typeof(IReadModelStore<TReadModel>)))
                .Returns(new[] { _readModelStoreMock.Object });

            _eventFlowConfigurationMock
                .Setup(c => c.PopulateReadModelEventPageSize)
                .Returns(ReadModelPageSize);

            _eventStoreMock
                .Setup(s => s.LoadAllEventsAsync(It.IsAny<GlobalPosition>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns<GlobalPosition, int, CancellationToken>((s, p, c) => Task.FromResult(GetEvents(s, p)));
            _readStoreManagerMock
                .Setup(m => m.ReadModelType)
                .Returns(typeof(TReadModel));
        }

        [Test]
        public async Task PurgeIsCalled()
        {
            // Act
            await Sut.PurgeAsync<TReadModel>(CancellationToken.None).ConfigureAwait(false);

            // Assert
            _readModelStoreMock.Verify(s => s.DeleteAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task PopulateCallsApplyDomainEvents()
        {
            // Arrange
            ArrangeEventStore(Many<ThingyPingEvent>(6));

            // Act
            await Sut.PopulateAsync<TReadModel>(CancellationToken.None).ConfigureAwait(false);

            // Assert
            _readStoreManagerMock.Verify(
                s => s.UpdateReadStoresAsync(
                    It.Is<IReadOnlyCollection<IDomainEvent>>(l => l.Count == ReadModelPageSize),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Test]
        public async Task UnwantedEventsAreFiltered()
        {
            // Arrange
            var events = new IAggregateEvent[]
            {
                A<ThingyPingEvent>(),
                A<ThingyDomainErrorAfterFirstEvent>(),
                A<ThingyPingEvent>(),
            };
            ArrangeEventStore(events);

            // Act
            await Sut.PopulateAsync<TReadModel>(CancellationToken.None).ConfigureAwait(false);

            // Assert
            _readStoreManagerMock
                .Verify(
                    s => s.UpdateReadStoresAsync(
                        It.Is<IReadOnlyCollection<IDomainEvent>>(l => l.Count == 2 && l.All(e => e.EventType == typeof(ThingyPingEvent))),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
        }

        private AllEventsPage GetEvents(GlobalPosition globalPosition, int pageSize)
        {
            var startPosition = globalPosition.IsStart
                ? 1
                : int.Parse(globalPosition.Value);

            var events = _eventStoreData
                .Skip(Math.Max(startPosition - 1, 0))
                .Take(pageSize)
                .ToList();

            var nextPosition = Math.Min(Math.Max(startPosition, 1) + pageSize, _eventStoreData.Count + 1);

            return new AllEventsPage(new GlobalPosition(nextPosition.ToString()), events);
        }

        private void ArrangeEventStore(IEnumerable<IAggregateEvent> aggregateEvents)
        {
            ArrangeEventStore(aggregateEvents.Select(e => ToDomainEvent(e)));
        }

        private void ArrangeEventStore(IEnumerable<IDomainEvent> domainEvents)
        {
            _eventStoreData = domainEvents.ToList();
        }
    }
}
