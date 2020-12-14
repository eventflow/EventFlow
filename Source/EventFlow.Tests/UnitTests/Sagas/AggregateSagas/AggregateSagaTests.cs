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

using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using EventFlow.Exceptions;
using EventFlow.Sagas;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.Sagas;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.Tests.UnitTests.Sagas.AggregateSagas
{
    [Category(Categories.Unit)]
    public class AggregateSagaTests : TestsFor<ThingySaga>
    {
        private ThingySagaId _thingySagaId;
        private Mock<ICommandBus> _commandBus;
        private Mock<ISagaContext> _sagaContext;

        [SetUp]
        public async Task SetUp()
        {
            _thingySagaId = A<ThingySagaId>();
            _commandBus = InjectMock<ICommandBus>();
            _sagaContext = InjectMock<ISagaContext>();

            await Sut.HandleAsync(
                A<DomainEvent<ThingyAggregate, ThingyId, ThingySagaStartRequestedEvent>>(),
                _sagaContext.Object,
                CancellationToken.None);
        }

        [Test]
        public async Task AggregateSaga_PublishAsync_ExecutionResultIsSuccessTrueDoesNotThrow()
        {
            // Arrange
            _commandBus.Setup(
                a => a.PublishAsync(
                    It.IsAny<ICommand<ThingyAggregate, ThingyId, IExecutionResult>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(ExecutionResult.Success()));
            await Sut.HandleAsync(
                A<DomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>(),
                _sagaContext.Object,
                CancellationToken.None);
            var exception = (Exception)null;

            // Act
            try
            {
                await Sut.PublishAsync(
                    _commandBus.Object,
                    CancellationToken.None);
            }
            catch (Exception e)
            {
                exception = e;
            }

            // Assert
            exception.Should().BeNull();
        }

        [Test]
        public async Task AggregateSaga_PublishAsync_ExecutionResultIsNullDoesNotThrow()
        {
            // Arrange
            _commandBus.Setup(
                a => a.PublishAsync(
                    It.IsAny<ICommand<ThingyAggregate, ThingyId, IExecutionResult>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<IExecutionResult>(null));
            await Sut.HandleAsync(
                A<DomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>(),
                _sagaContext.Object,
                CancellationToken.None);
            var exception = (Exception)null;

            // Act
            try
            {
                await Sut.PublishAsync(
                    _commandBus.Object,
                    CancellationToken.None);
            }
            catch (Exception e)
            {
                exception = e;
            }

            // Assert
            exception.Should().BeNull();
        }

        [Test]
        public async Task AggregateSaga_PublishAsync_ExecutionResultIsSuccessFalseDisableThrow()
        {
            // Arrange
            Sut.GetType()
                .GetProperty("ThrowExceptionsOnFailedPublish", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty)
                .SetValue(Sut, false);
            var message = Guid.NewGuid().ToString();
            _commandBus.Setup(
                a => a.PublishAsync(
                    It.IsAny<ICommand<ThingyAggregate, ThingyId, IExecutionResult>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(ExecutionResult.Failed(message)));
            await Sut.HandleAsync(
                A<DomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>(),
                _sagaContext.Object,
                CancellationToken.None);
            var exception = (Exception)null;

            // Act
            try
            {
                await Sut.PublishAsync(
                    _commandBus.Object,
                    CancellationToken.None);
            }
            catch (Exception e)
            {
                exception = e;
            }

            // Assert
            exception.Should().BeNull();
        }

        [Test]
        public async Task AggregateSaga_PublishAsync_ExecutionResultIsSuccessFalseThrows()
        {
            // Arrange
            var message = Guid.NewGuid().ToString();
            _commandBus.Setup(
                a => a.PublishAsync(
                    It.IsAny<ICommand<ThingyAggregate, ThingyId, IExecutionResult>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(ExecutionResult.Failed(message)));
            await Sut.HandleAsync(
                A<DomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>(),
                _sagaContext.Object,
                CancellationToken.None);
            var exception = (Exception)null;

            // Act
            try
            {
                await Sut.PublishAsync(
                    _commandBus.Object,
                    CancellationToken.None);
            }
            catch (Exception e)
            {
                exception = e;
            }

            // Assert
            exception.Should().NotBeNull();
            exception.Should().BeAssignableTo<SagaPublishException>();
            var sagaPublishException = exception as SagaPublishException;
            sagaPublishException.InnerExceptions.Count.Should().Be(1);
            var innerException = sagaPublishException.InnerExceptions[0];
            innerException.Should().BeAssignableTo<CommandException>();
            var commandException = innerException as CommandException;
            commandException.Message.Should().Contain(message);
            commandException.Message.Should().Contain(Sut.Id.ToString());
            commandException.CommandType.Should().Be(typeof(ThingyAddMessageCommand));
            commandException.ExecutionResult.Should().NotBeNull();
            commandException.ExecutionResult.IsSuccess.Should().BeFalse();
            commandException.ExecutionResult.ToString().Should().Contain(message);
        }

        [Test]
        public async Task AggregateSaga_PublishAsync_ExecutionResultIsSuccessFalseThrowsTwice()
        {
            // Arrange
            var message = Guid.NewGuid().ToString();
            _commandBus.Setup(
                a => a.PublishAsync(
                    It.IsAny<ICommand<ThingyAggregate, ThingyId, IExecutionResult>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(ExecutionResult.Failed(message)));
            await Sut.HandleAsync(
                A<DomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>(),
                _sagaContext.Object,
                CancellationToken.None);
            await Sut.HandleAsync(
                A<DomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>(),
                _sagaContext.Object,
                CancellationToken.None);
            var exception = (Exception)null;

            // Act
            try
            {
                await Sut.PublishAsync(
                    _commandBus.Object,
                    CancellationToken.None);
            }
            catch (Exception e)
            {
                exception = e;
            }

            // Assert
            exception.Should().NotBeNull();
            exception.Should().BeAssignableTo<SagaPublishException>();
            var sagaPublishException = exception as SagaPublishException;
            sagaPublishException.InnerExceptions.Count.Should().Be(2);
            foreach (var innerException in sagaPublishException.InnerExceptions)
            {
                innerException.Should().BeAssignableTo<CommandException>();
                var commandException = innerException as CommandException;
                commandException.Message.Should().Contain(message);
                commandException.Message.Should().Contain(Sut.Id.ToString());
                commandException.CommandType.Should().Be(typeof(ThingyAddMessageCommand));
                commandException.ExecutionResult.Should().NotBeNull();
                commandException.ExecutionResult.IsSuccess.Should().BeFalse();
                commandException.ExecutionResult.ToString().Should().Contain(message);
            }
        }

        [Test]
        public async Task AggregateSaga_PublishAsync_ExceptionIsWrapped()
        {
            // Arrange
            var message = Guid.NewGuid().ToString();
            _commandBus.Setup(
                a => a.PublishAsync(
                    It.IsAny<ICommand<ThingyAggregate, ThingyId, IExecutionResult>>(),
                    It.IsAny<CancellationToken>()))
                .Callback(() => throw new ApplicationException(message));
            await Sut.HandleAsync(
                A<DomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>(),
                _sagaContext.Object,
                CancellationToken.None);
            var exception = (Exception)null;

            // Act
            try
            {
                await Sut.PublishAsync(
                    _commandBus.Object,
                    CancellationToken.None);
            }
            catch (Exception e)
            {
                exception = e;
            }

            // Assert
            exception.Should().NotBeNull();
            exception.Should().BeAssignableTo<SagaPublishException>();
            var sagaPublishException = exception as SagaPublishException;
            sagaPublishException.InnerExceptions.Count.Should().Be(1);
            var innerException = sagaPublishException.InnerExceptions[0];
            innerException.Should().BeAssignableTo<CommandException>();
            var commandException = innerException as CommandException;
            commandException.Message.Should().Contain(message);
            commandException.Message.Should().Contain(Sut.Id.ToString());
            commandException.CommandType.Should().Be(typeof(ThingyAddMessageCommand));
            commandException.InnerException.Should().NotBeNull();
            commandException.InnerException.Should().BeAssignableTo<ApplicationException>();
            commandException.InnerException.Message.Should().Be(message);
        }

        [Test]
        public async Task AggregateSaga_PublishAsync_TwoExceptionsAreWrapped()
        {
            // Arrange
            var message = Guid.NewGuid().ToString();
            _commandBus.Setup(
                a => a.PublishAsync(
                    It.IsAny<ICommand<ThingyAggregate, ThingyId, IExecutionResult>>(),
                    It.IsAny<CancellationToken>()))
                .Callback(() => throw new ApplicationException(message));
            await Sut.HandleAsync(
                A<DomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>(),
                _sagaContext.Object,
                CancellationToken.None);
            await Sut.HandleAsync(
                A<DomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>(),
                _sagaContext.Object,
                CancellationToken.None);
            var exception = (Exception)null;

            // Act
            try
            {
                await Sut.PublishAsync(
                    _commandBus.Object,
                    CancellationToken.None);
            }
            catch (Exception e)
            {
                exception = e;
            }

            // Assert
            exception.Should().NotBeNull();
            exception.Should().BeAssignableTo<SagaPublishException>();
            var sagaPublishException = exception as SagaPublishException;
            sagaPublishException.InnerExceptions.Count.Should().Be(2);
            foreach (var innerException in sagaPublishException.InnerExceptions)
            {
                innerException.Should().BeAssignableTo<CommandException>();
                var commandException = innerException as CommandException;
                commandException.Message.Should().Contain(message);
                commandException.Message.Should().Contain(Sut.Id.ToString());
                commandException.CommandType.Should().Be(typeof(ThingyAddMessageCommand));
                commandException.InnerException.Should().NotBeNull();
                commandException.InnerException.Should().BeAssignableTo<ApplicationException>();
                commandException.InnerException.Message.Should().Be(message);
            }
        }
    }
}