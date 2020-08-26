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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Sagas;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.Sagas;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Sagas
{
    [Category(Categories.Unit)]
    public class DispatchToSagasTests : TestsFor<DispatchToSagas>
    {
        private Mock<IResolver> _resolverMock;
        private Mock<ISagaStore> _sagaStoreMock;
        private Mock<ISagaDefinitionService> _sagaDefinitionServiceMock;
        private Mock<ISagaErrorHandler> _sagaErrorHandlerMock;
        private Mock<ISagaUpdater> _sagaUpdaterMock;
        private Mock<ISagaLocator> _sagaLocatorMock;

        [SetUp]
        public void SetUp()
        {
            var sagaType = typeof(ThingySaga);

            _resolverMock = InjectMock<IResolver>();
            _sagaStoreMock = InjectMock<ISagaStore>();
            _sagaDefinitionServiceMock = InjectMock<ISagaDefinitionService>();
            _sagaErrorHandlerMock = InjectMock<ISagaErrorHandler>();

            _sagaUpdaterMock = new Mock<ISagaUpdater>();
            _sagaLocatorMock = new Mock<ISagaLocator>();

            _resolverMock
                .Setup(r => r.Resolve(It.Is<Type>(t => typeof(ISagaLocator).IsAssignableFrom(t))))
                .Returns(_sagaLocatorMock.Object);
            _resolverMock
                .Setup(r => r.Resolve(It.Is<Type>(t => typeof(ISagaUpdater).IsAssignableFrom(t))))
                .Returns(_sagaUpdaterMock.Object);
            _sagaDefinitionServiceMock
                .Setup(d => d.GetSagaDetails(It.IsAny<Type>()))
                .Returns(new[] {SagaDetails.From(sagaType)});
            _sagaLocatorMock
                .Setup(s => s.LocateSagaAsync(It.IsAny<IDomainEvent>(), CancellationToken.None))
                .Returns(() => Task.FromResult<ISagaId>(new ThingySagaId(string.Empty)));
        }

        [Test]
        public async Task SagaUpdaterIsInvokedCorrectly()
        {
            // Arrange
            const int domainEventCount = 4;
            var sagaMock = Arrange_Woking_SagaStore(SagaState.Running);
            var domainEvents = ManyDomainEvents<ThingyPingEvent>(domainEventCount);

            // Act
            await Sut.ProcessAsync(domainEvents, CancellationToken.None).ConfigureAwait(false);

            // Assert
            _sagaUpdaterMock.Verify(
                u => u.ProcessAsync(sagaMock.Object, It.IsAny<IDomainEvent>(), It.IsAny<ISagaContext>(), It.IsAny<CancellationToken>()),
                Times.Exactly(domainEventCount));
        }

        [Test]
        public async Task SagaStoreReceivesEventIdAsSourceId()
        {
            // Arrange
            var sagaMock = Arrange_Woking_SagaStore(SagaState.Running);
            var domainEvent = ADomainEvent<ThingyPingEvent>();

            // Act
            await Sut.ProcessAsync(new[] { domainEvent }, CancellationToken.None).ConfigureAwait(false);

            // Assert
            _sagaStoreMock.Verify(a => a.UpdateAsync(It.IsAny<ISagaId>(), It.IsAny<Type>(),
                domainEvent.Metadata.EventId,
                It.IsAny<Func<ISaga, CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task SagaErrorHandlerIsInvokedCorrectly()
        {
            // Arrange
            const int domainEventCount = 1;
            Arrange_Working_ErrorHandler();
            var expectedException = Arrange_Faulty_SagaStore();
            var domainEvents = ManyDomainEvents<ThingyPingEvent>(domainEventCount);

            // Act
            await Sut.ProcessAsync(domainEvents, CancellationToken.None).ConfigureAwait(false);

            // Assert
            _sagaErrorHandlerMock.Verify(
                m => m.HandleAsync(
                    It.IsAny<ISagaId>(),
                    It.IsAny<SagaDetails>(),
                    expectedException,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public void UnhandledSagaErrorIsThrown()
        {
            // Arrange
            const int domainEventCount = 1;
            Arrange_Working_ErrorHandler(false);
            var expectedException = Arrange_Faulty_SagaStore();
            var domainEvents = ManyDomainEvents<ThingyPingEvent>(domainEventCount);

            // Act
            var thrownException = Assert.ThrowsAsync<Exception>(
                async () => await Sut.ProcessAsync(domainEvents, CancellationToken.None).ConfigureAwait(false));

            // Assert
            thrownException.Should().BeSameAs(expectedException);
        }

        private Exception Arrange_Faulty_SagaStore()
        {
            var exception = A<Exception>();

            _sagaStoreMock
                .Setup(s => s.UpdateAsync(
                    It.IsAny<ISagaId>(),
                    It.IsAny<Type>(),
                    It.IsAny<ISourceId>(),
                    It.IsAny<Func<ISaga, CancellationToken, Task>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            return exception;
        }

        private void Arrange_Working_ErrorHandler(bool handlesIt = true)
        {
            _sagaErrorHandlerMock
                .Setup(m => m.HandleAsync(
                    It.IsAny<ISagaId>(),
                    It.IsAny<SagaDetails>(),
                    It.IsAny<Exception>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(handlesIt);
        }

        private Mock<ISaga> Arrange_Woking_SagaStore(SagaState sagaState = SagaState.New)
        {
            var sagaMock = new Mock<ISaga>();

            sagaMock
                .Setup(s => s.State)
                .Returns(sagaState);

            _sagaStoreMock
                .Setup(s => s.UpdateAsync(
                    It.IsAny<ISagaId>(),
                    It.IsAny<Type>(),
                    It.IsAny<ISourceId>(),
                    It.IsAny<Func<ISaga, CancellationToken, Task>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<ISagaId, Type, ISourceId, Func<ISaga, CancellationToken, Task>, CancellationToken>(
                    (id, details, arg3, arg4, arg5) => arg4(sagaMock.Object, CancellationToken.None))
                .ReturnsAsync(sagaMock.Object);

            return sagaMock;
        }
    }
}