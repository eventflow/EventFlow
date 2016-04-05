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
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using EventFlow.TestHelpers.Extensions;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;

namespace EventFlow.Tests.UnitTests
{
    [TestFixture]
    [Category(Categories.Unit)]
    public class CommandBusTests : TestsFor<CommandBus>
    {
        private Mock<IEventStore> _eventStoreMock;
        private Mock<IResolver> _resolverMock;
        private ThingyAggregate _thingyAggregate;

        [SetUp]
        public void SetUp()
        {
            Fixture.Inject<ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy>>(
                new TransientFaultHandler<IOptimisticConcurrencyRetryStrategy>(
                    Fixture.Create<ILog>(),
                    new OptimisticConcurrencyRetryStrategy(new EventFlowConfiguration())));

            _resolverMock = InjectMock<IResolver>();
            _eventStoreMock = InjectMock<IEventStore>();
            _thingyAggregate = new ThingyAggregate(ThingyId.New);

            _eventStoreMock
                .Setup(s => s.LoadAggregateAsync<ThingyAggregate, ThingyId>(It.IsAny<ThingyId>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(_thingyAggregate));
        }

        [Test]
        public void RetryForOptimisticConcurrencyExceptionsAreDone()
        {
            // Arrange
            ArrangeCommandHandlerExists<ThingyAggregate, ThingyId, ISourceId, ThingyDomainErrorAfterFirstCommand>();
            _eventStoreMock
                .Setup(s => s.StoreAsync<ThingyAggregate, ThingyId>(It.IsAny<ThingyId>(), It.IsAny<IReadOnlyCollection<IUncommittedEvent>>(), It.IsAny<ISourceId>(), It.IsAny<CancellationToken>()))
                .Throws(new OptimisticConcurrencyException(string.Empty, null));

            // Act
            Assert.Throws<OptimisticConcurrencyException>(async () => await Sut.PublishAsync(new ThingyDomainErrorAfterFirstCommand(ThingyId.New)).ConfigureAwait(false));

            // Assert
            _eventStoreMock.Verify(
                s => s.StoreAsync<ThingyAggregate, ThingyId>(It.IsAny<ThingyId>(), It.IsAny<IReadOnlyCollection<IUncommittedEvent>>(), It.IsAny<ISourceId>(), It.IsAny<CancellationToken>()),
                Times.Exactly(6));
        }

        [Test]
        public void DuplicateOperationExceptionIsThrowsIfSourceAlreadyApplied()
        {
            // Arrange
            var pingEvent = ToDomainEvent(new ThingyPingEvent(PingId.New));
            ArrangeWorkingEventStore();
            ArrangeCommandHandlerExists(new ThingyPingCommandHandler());
            _thingyAggregate.ApplyEvents(new [] { pingEvent });

            // Act + Assert
            Assert.Throws<DuplicateOperationException>(async () => await Sut.PublishAsync(new ThingyPingCommand(ThingyId.New, pingEvent.Metadata.SourceId, PingId.New)).ConfigureAwait(false));
        }

        [Test]
        public async Task CommandHandlerIsInvoked()
        {
            // Arrange
            ArrangeWorkingEventStore();
            var commandHandler = ArrangeCommandHandlerExists<ThingyAggregate, ThingyId, ISourceId, ThingyPingCommand>();

            // Act
            await Sut.PublishAsync(new ThingyPingCommand(ThingyId.New, PingId.New)).ConfigureAwait(false);

            // Assert
            commandHandler.Verify(h => h.ExecuteAsync(It.IsAny<ThingyAggregate>(), It.IsAny<ThingyPingCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        private void ArrangeWorkingEventStore()
        {
            _eventStoreMock
                .Setup(s => s.StoreAsync<ThingyAggregate, ThingyId>(It.IsAny<ThingyId>(), It.IsAny<IReadOnlyCollection<IUncommittedEvent>>(), It.IsAny<ISourceId>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<IReadOnlyCollection<IDomainEvent<ThingyAggregate, ThingyId>>>(Many<IDomainEvent<ThingyAggregate, ThingyId>>()));
        }

        private void ArrangeCommandHandlerExists<TAggregate, TIdentity, TSourceIdentity, TCommand>(
            ICommandHandler<TAggregate, TIdentity, TSourceIdentity, TCommand> commandHandler)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TSourceIdentity : ISourceId
            where TCommand : ICommand<TAggregate, TIdentity, TSourceIdentity>
        {
            _resolverMock
                .Setup(r => r.ResolveAll(typeof(ICommandHandler<TAggregate, TIdentity, TSourceIdentity, TCommand>)))
                .Returns(new[] { commandHandler });
        }

        private Mock<ICommandHandler<TAggregate, TIdentity, TSourceIdentity, TCommand>> ArrangeCommandHandlerExists<TAggregate, TIdentity, TSourceIdentity, TCommand>()
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TSourceIdentity : ISourceId
            where TCommand : ICommand<TAggregate, TIdentity, TSourceIdentity>
        {
            var mock = new Mock<ICommandHandler<TAggregate, TIdentity, TSourceIdentity, TCommand>>();
            ArrangeCommandHandlerExists(mock.Object);
            return mock;
        }
    }
}
