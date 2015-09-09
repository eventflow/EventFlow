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
using EventFlow.TestHelpers.Aggregates.Test.Events;
using EventFlow.TestHelpers.Aggregates.Test.ValueObjects;
using EventFlow.TestHelpers.Extensions;
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
        private TestAggregate _testAggregate;

        [SetUp]
        public void SetUp()
        {
            Fixture.Inject<ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy>>(
                new TransientFaultHandler<IOptimisticConcurrencyRetryStrategy>(
                    Fixture.Create<ILog>(),
                    new OptimisticConcurrencyRetryStrategy(new EventFlowConfiguration())));

            _resolverMock = InjectMock<IResolver>();
            _eventStoreMock = InjectMock<IEventStore>();
            _testAggregate = new TestAggregate(TestId.New);

            _eventStoreMock
                .Setup(s => s.LoadAggregateAsync<TestAggregate, TestId>(It.IsAny<TestId>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(_testAggregate));
        }

        [Test]
        public void RetryForOptimisticConcurrencyExceptionsAreDone()
        {
            // Arrange
            ArrangeCommandHandlerExists<TestAggregate, TestId, DomainErrorAfterFirstCommand>();
            _eventStoreMock
                .Setup(s => s.StoreAsync<TestAggregate, TestId>(It.IsAny<TestId>(), It.IsAny<IReadOnlyCollection<IUncommittedEvent>>(), It.IsAny<SourceId>(), It.IsAny<CancellationToken>()))
                .Throws(new OptimisticConcurrencyException(string.Empty, null));

            // Act
            Assert.Throws<OptimisticConcurrencyException>(async () => await Sut.PublishAsync(new DomainErrorAfterFirstCommand(TestId.New)).ConfigureAwait(false));

            // Assert
            _eventStoreMock.Verify(
                s => s.StoreAsync<TestAggregate, TestId>(It.IsAny<TestId>(), It.IsAny<IReadOnlyCollection<IUncommittedEvent>>(), It.IsAny<SourceId>(), It.IsAny<CancellationToken>()),
                Times.Exactly(5));
        }

        [Test]
        public void DuplicateOperationExceptionIsThrowsIfSourceAlreadyApplied()
        {
            // Arrange
            var pingEvent = ToDomainEvent(new PingEvent(PingId.New));
            ArrangeWorkingEventStore();
            ArrangeCommandHandlerExists(new PingCommandHandler());
            _testAggregate.ApplyEvents(new [] { pingEvent });

            // Act + Assert
            Assert.Throws<DuplicateOperationException>(async () => await Sut.PublishAsync(new PingCommand(TestId.New, pingEvent.Metadata.SourceId, PingId.New)).ConfigureAwait(false));
        }

        [Test]
        public async Task CommandHandlerIsInvoked()
        {
            // Arrange
            ArrangeWorkingEventStore();
            var commandHandler = ArrangeCommandHandlerExists<TestAggregate, TestId, PingCommand>();

            // Act
            await Sut.PublishAsync(new PingCommand(TestId.New, PingId.New)).ConfigureAwait(false);

            // Assert
            commandHandler.Verify(h => h.ExecuteAsync(It.IsAny<TestAggregate>(), It.IsAny<PingCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        private void ArrangeWorkingEventStore()
        {
            _eventStoreMock
                .Setup(s => s.StoreAsync<TestAggregate, TestId>(It.IsAny<TestId>(), It.IsAny<IReadOnlyCollection<IUncommittedEvent>>(), It.IsAny<SourceId>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<IReadOnlyCollection<IDomainEvent<TestAggregate, TestId>>>(Many<IDomainEvent<TestAggregate, TestId>>()));
        }

        private void ArrangeCommandHandlerExists<TAggregate, TIdentity, TCommand>(
            ICommandHandler<TAggregate, TIdentity, TCommand> commandHandler)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TCommand : ICommand<TAggregate, TIdentity>
        {
            _resolverMock
                .Setup(r => r.ResolveAll(typeof(ICommandHandler<TAggregate, TIdentity, TCommand>)))
                .Returns(new[] { commandHandler });
        }

        private Mock<ICommandHandler<TAggregate, TIdentity, TCommand>> ArrangeCommandHandlerExists<TAggregate, TIdentity, TCommand>()
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TCommand : ICommand<TAggregate, TIdentity>
        {
            var mock = new Mock<ICommandHandler<TAggregate, TIdentity, TCommand>>();
            ArrangeCommandHandlerExists(mock.Object);
            return mock;
        }
    }
}
