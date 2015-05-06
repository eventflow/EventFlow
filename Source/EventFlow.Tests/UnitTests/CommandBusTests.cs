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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Core.RetryStrategies;
using EventFlow.EventStores;
using EventFlow.Exceptions;
using EventFlow.Logs;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Test;
using EventFlow.TestHelpers.Aggregates.Test.Commands;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;

namespace EventFlow.Tests.UnitTests
{
    [TestFixture]
    public class CommandBusTests : TestsFor<CommandBus>
    {
        private Mock<IEventStore> _eventStoreMock;
        private Mock<IResolver> _resolverMock;

        [SetUp]
        public void SetUp()
        {
            var resolver = InjectMock<IResolver>();

            Fixture.Inject<ITransientFaultHandler>(new TransientFaultHandler(resolver.Object, Fixture.Create<ILog>()));

            _resolverMock = InjectMock<IResolver>();
            _eventStoreMock = InjectMock<IEventStore>();

            _eventStoreMock
                .Setup(s => s.LoadAggregateAsync<TestAggregate, TestId>(It.IsAny<TestId>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new TestAggregate(TestId.New)));
            resolver
                .Setup(r => r.Resolve<IOptimisticConcurrencyRetryStrategy>())
                .Returns(new OptimisticConcurrencyRetryStrategy(new EventFlowConfiguration()));
        }

        [Test]
        public void RetryForOptimisticConcurrencyExceptionsAreDone()
        {
            // Arrange
            ArrangeCommandHandlerExists<TestAggregate, TestId, DomainErrorAfterFirstCommand>();
            _eventStoreMock
                .Setup(s => s.StoreAsync<TestAggregate, TestId>(It.IsAny<TestId>(), It.IsAny<IReadOnlyCollection<IUncommittedEvent>>(), It.IsAny<CancellationToken>()))
                .Throws(new OptimisticConcurrencyException(string.Empty, null));

            // Act
            Assert.Throws<OptimisticConcurrencyException>(async () => await Sut.PublishAsync(new DomainErrorAfterFirstCommand(TestId.New), CancellationToken.None).ConfigureAwait(false));

            // Assert
            _eventStoreMock.Verify(
                s => s.StoreAsync<TestAggregate, TestId>(It.IsAny<TestId>(), It.IsAny<IReadOnlyCollection<IUncommittedEvent>>(), It.IsAny<CancellationToken>()),
                Times.Exactly(5));
        }

        [Test]
        public async Task CommandHandlerIsInvoked()
        {
            // Arrange
            ArrangeWorkingEventStore();
            var commandHandler = ArrangeCommandHandlerExists<TestAggregate, TestId, PingCommand>();

            // Act
            await Sut.PublishAsync(new PingCommand(TestId.New), CancellationToken.None).ConfigureAwait(false);

            // Assert
            commandHandler.Verify(h => h.ExecuteAsync(It.IsAny<TestAggregate>(), It.IsAny<PingCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        private void ArrangeWorkingEventStore()
        {
            _eventStoreMock
                .Setup(s => s.StoreAsync<TestAggregate, TestId>(It.IsAny<TestId>(), It.IsAny<IReadOnlyCollection<IUncommittedEvent>>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<IReadOnlyCollection<IDomainEvent>>(Many<IDomainEvent>()));
        }

        private Mock<ICommandHandler<TAggregate, TIdentity, TCommand>> ArrangeCommandHandlerExists<TAggregate, TIdentity, TCommand>()
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TCommand : ICommand<TAggregate, TIdentity>
        {
            var mock = new Mock<ICommandHandler<TAggregate, TIdentity, TCommand>>();
            _resolverMock
                .Setup(r => r.ResolveAll(typeof (ICommandHandler<TAggregate, TIdentity, TCommand>)))
                .Returns(new[] {mock.Object});
            return mock;
        }
    }
}
