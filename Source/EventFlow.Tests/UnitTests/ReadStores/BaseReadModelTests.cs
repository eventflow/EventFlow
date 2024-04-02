// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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
        protected const int ReadModelPageSize = 3;
        protected const int PopulateReadModelPageSize = 6;

        private Mock<IReadModelStore<TReadModel>> _readModelStoreMock;
        protected Mock<IReadStoreManager<IReadModel>> _readStoreManagerMock;
        private Mock<IEventFlowConfiguration> _eventFlowConfigurationMock;
        protected Mock<IEventStore> _eventStoreMock;
        private Mock<IServiceProvider> _serviceProviderMock;
        private List<IDomainEvent> _eventStoreData;

        [SetUp]
        public void SetUp()
        {
            Inject<IEventUpgradeContextFactory>(new EventUpgradeContextFactory());

            _eventStoreMock = InjectMock<IEventStore>();
            _eventStoreData = null;
            _serviceProviderMock = InjectMock<IServiceProvider>();
            _readModelStoreMock = new Mock<IReadModelStore<TReadModel>>();
            _readStoreManagerMock = new Mock<IReadStoreManager<IReadModel>>();
            _eventFlowConfigurationMock = InjectMock<IEventFlowConfiguration>();

            _serviceProviderMock
                .Setup(r => r.GetService(typeof(IEnumerable<IReadStoreManager>)))
                .Returns(new[] { _readStoreManagerMock.Object });
            _serviceProviderMock
                .Setup(r => r.GetService(typeof(IEnumerable<IReadModelStore<TReadModel>>)))
                .Returns(new[] { _readModelStoreMock.Object });

            _eventFlowConfigurationMock
                .Setup(c => c.LoadReadModelEventPageSize)
                .Returns(ReadModelPageSize);

            _eventFlowConfigurationMock
                .Setup(c => c.PopulateReadModelEventPageSize)
                .Returns(PopulateReadModelPageSize);

            _eventStoreMock
                .Setup(s => s.LoadAllEventsAsync(It.IsAny<GlobalPosition>(), It.IsAny<int>(), It.IsAny<IEventUpgradeContext>(), It.IsAny<CancellationToken>()))
                .Returns<GlobalPosition, int, IEventUpgradeContext, CancellationToken>((s, p, uc, c) => Task.FromResult(GetEvents(s, p)));
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
            _eventStoreMock.Verify(
                s => s.LoadAllEventsAsync(It.IsAny<GlobalPosition>(), It.Is<int>(i => i == ReadModelPageSize), It.IsAny<IEventUpgradeContext>(), It.IsAny<CancellationToken>()),
                Times.Exactly(3)); ;

            _readStoreManagerMock.Verify(
                s => s.UpdateReadStoresAsync(
                    It.Is<IReadOnlyCollection<IDomainEvent>>(l => l.Count == PopulateReadModelPageSize),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(1));
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

        protected void ArrangeEventStore(IEnumerable<IAggregateEvent> aggregateEvents)
        {
            ArrangeEventStore(aggregateEvents.Select(e => ToDomainEvent(e)));
        }

        private void ArrangeEventStore(IEnumerable<IDomainEvent> domainEvents)
        {
            _eventStoreData = domainEvents.ToList();
        }

        protected Mock<IReadStoreManager<IReadModel>> ArrangeExtraReadModelManager<TExtraReadModel>()
            where TExtraReadModel : class, IReadModel
        {
            var extraManager = new Mock<IReadStoreManager<IReadModel>>();
            var extraReadModel = new Mock<IReadModelStore<TExtraReadModel>>();

            _serviceProviderMock
                .Setup(r => r.GetService(typeof(IEnumerable<IReadStoreManager>)))
                .Returns(new[] { _readStoreManagerMock.Object, extraManager.Object });

            _serviceProviderMock
                .Setup(r => r.GetService(typeof(IEnumerable<IReadModelStore<TExtraReadModel>>)))
                .Returns(new[] { extraReadModel.Object });

            extraManager
                .Setup(m => m.ReadModelType)
                .Returns(typeof(TExtraReadModel));

            return extraManager;
        }
    }
}
