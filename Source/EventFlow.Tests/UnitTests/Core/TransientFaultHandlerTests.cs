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
using EventFlow.Core;
using EventFlow.Logs;
using EventFlow.TestHelpers;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Core
{
    [Category(Categories.Unit)]
    public class TransientFaultHandlerTests : TestsFor<TransientFaultHandler<IRetryStrategy>>
    {
        private Mock<IRetryStrategy> _retryStrategyMock;
        private Mock<ILog> _logMock;

        [SetUp]
        public void SetUp()
        {
            _logMock = new Mock<ILog>();
            _retryStrategyMock = new Mock<IRetryStrategy>();
        }

        [Test]
        public async Task WorkingActionsSucceed()
        {
            // Arrange
            var action = CreateFailingFunction(Task.FromResult(A<int>()));

            // Act
            await Sut.TryAsync(c => action.Object(), A<Label>(), CancellationToken.None);

            // Assert
            action.Verify(f => f(), Times.Once);
            _retryStrategyMock.Verify(rs => rs.ShouldThisBeRetried(It.IsAny<Exception>(), It.IsAny<TimeSpan>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task Result()
        {
            // Arrange
            ArrangeRetryStrategy(2);
            var expectedResult = A<int>();
            var action = CreateFailingFunction(Task.FromResult(expectedResult),
                new ArgumentException(),
                new InvalidOperationException());

            // Act
            var result = await Sut.TryAsync(c => action.Object(), A<Label>(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            result.Should().Be(expectedResult);
            action.Verify(f => f(), Times.Exactly(3));
        }

        [TestCase(0, "ArgumentException")]
        [TestCase(1, "InvalidOperationException")]
        [TestCase(2, "DivideByZeroException")]
        public async Task ThrownExceptionIsAsExcpedted(int numberOfRetries, string expectedExceptionName)
        {
            // Arrange
            ArrangeRetryStrategy(numberOfRetries);
            var action = CreateFailingFunction(Task.FromResult(A<int>()),
                new ArgumentException(),
                new InvalidOperationException(),
                new DivideByZeroException());

            // Act
            Exception thrownException = null;
            try
            {
                await Sut.TryAsync(c => action.Object(), A<Label>(), CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                thrownException = exception;
            }

            // Assert
            thrownException.Should().NotBeNull();
            thrownException.GetType().Name.Should().Be(expectedExceptionName);
            action.Verify(f => f(), Times.Exactly(numberOfRetries + 1));
        }

        protected override TransientFaultHandler<IRetryStrategy> CreateSut()
        {
            return new TransientFaultHandler<IRetryStrategy>(
                _logMock.Object,
                _retryStrategyMock.Object);
        }

        private void ArrangeRetryStrategy(int numberOfRetries)
        {
            _retryStrategyMock
                .Setup(rs => rs.ShouldThisBeRetried(It.IsAny<Exception>(), It.IsAny<TimeSpan>(), It.Is<int>(i => i != numberOfRetries)))
                .Returns(() => Retry.Yes);
            _retryStrategyMock
                .Setup(rs => rs.ShouldThisBeRetried(It.IsAny<Exception>(), It.IsAny<TimeSpan>(), It.Is<int>(i => i == numberOfRetries)))
                .Returns(() => Retry.No);
        }
    }
}
