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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Core.RetryStrategies;
using EventFlow.EventStores;
using EventFlow.Exceptions;
using EventFlow.Logs;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;

namespace EventFlow.Tests.UnitTests.Aggregates
{
    [Category(Categories.Unit)]
    public class AggregateStoreTests : TestsFor<AggregateStore>
    {
        private Mock<IEventStore> _eventStoreMock;
        private Mock<IAggregateFactory> _aggregateFactoryMock;

        [SetUp]
        public void SetUp()
        {
            Fixture.Inject<ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy>>(
                new TransientFaultHandler<IOptimisticConcurrencyRetryStrategy>(
                    Fixture.Create<ILog>(),
                    new OptimisticConcurrencyRetryStrategy(new EventFlowConfiguration())));

            _eventStoreMock = InjectMock<IEventStore>();
            _aggregateFactoryMock = InjectMock<IAggregateFactory>();

            _aggregateFactoryMock
                .Setup(f => f.CreateNewAggregateAsync<ThingyAggregate, ThingyId>(It.IsAny<ThingyId>()))
                .Returns(() => Task.FromResult(A<ThingyAggregate>()));
        }

        [Test]
        public void UpdateAsync_RetryForOptimisticConcurrencyExceptionsAreDone()
        {
            // Arrange
            Arrange_EventStore_LoadEventsAsync_ReturnsEvents();
            Arrange_EventStore_StoreAsync_ThrowsOptimisticConcurrencyException();

            // Act
            Assert.ThrowsAsync<OptimisticConcurrencyException>(async () => await Sut.UpdateAsync<ThingyAggregate, ThingyId>(A<ThingyId>(), A<ISourceId>(), NoOperationAsync, CancellationToken.None).ConfigureAwait(false));

            // Assert
            _eventStoreMock.Verify(
                s => s.StoreAsync<ThingyAggregate, ThingyId>(It.IsAny<ThingyId>(), It.IsAny<IReadOnlyCollection<IUncommittedEvent>>(), It.IsAny<ISourceId>(), It.IsAny<CancellationToken>()),
                Times.Exactly(6));
        }

        [Test]
        public void UpdateAsync_DuplicateOperationExceptionIsThrowsIfSourceAlreadyApplied()
        {
            // Arrange
            var domainEvents = ManyDomainEvents<ThingyPingEvent>(1).ToArray();
            var sourceId = domainEvents[0].Metadata.SourceId;
            Arrange_EventStore_LoadEventsAsync_ReturnsEvents(domainEvents);

            // Act
            Assert.ThrowsAsync<DuplicateOperationException>(async () => await Sut.UpdateAsync<ThingyAggregate, ThingyId>(A<ThingyId>(), sourceId, NoOperationAsync, CancellationToken.None).ConfigureAwait(false));
        }

        private void Arrange_EventStore_StoreAsync_ThrowsOptimisticConcurrencyException()
        {
            _eventStoreMock
                .Setup(s => s.StoreAsync<ThingyAggregate, ThingyId>(It.IsAny<ThingyId>(), It.IsAny<IReadOnlyCollection<IUncommittedEvent>>(), It.IsAny<ISourceId>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OptimisticConcurrencyException(string.Empty, null));
        }

        private void Arrange_EventStore_LoadEventsAsync_ReturnsEvents(params IDomainEvent<ThingyAggregate, ThingyId>[] domainEvents)
        {
            _eventStoreMock
                .Setup(s => s.LoadEventsAsync<ThingyAggregate, ThingyId>(It.IsAny<ThingyId>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IReadOnlyCollection<IDomainEvent<ThingyAggregate, ThingyId>>>(domainEvents));
        }

        private static Task NoOperationAsync(ThingyAggregate thingyAggregate, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }
    }
}