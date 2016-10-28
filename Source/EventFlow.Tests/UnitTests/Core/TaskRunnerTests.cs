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

using System;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Logging;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Extensions;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Core
{
    [Category(Categories.Unit)]
    public class TaskRunnerTests : TestsFor<TaskRunner>
    {
        private Mock<ILog> _logMock;

        [Test]
        public void NoExceptionsAreThrownIfCancellationIsAlreadyRequested()
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                // Arrange
                cancellationTokenSource.Cancel();

                // Act
                Assert.DoesNotThrow(() => Sut.Run(A<Label>(), c => Task.FromResult(0), cancellationTokenSource.Token));
            }

            // Assert
            _logMock.VerifyNoErrorsLogged();
        }

        [Test]
        public void TaskIsInvoked()
        {
            // Arrange
            var autoResetEvent = new AutoResetEvent(false);
            var hasRun = false;

            // Act
            Sut.Run(
                A<Label>(),
                async c =>
                    {
                        await Task.Delay(10, c).ConfigureAwait(false);
                        hasRun = true;
                        autoResetEvent.Set();
                    },
                CancellationToken.None);

            // Assert
            autoResetEvent.WaitOne(TimeSpan.FromSeconds(10));
            hasRun.Should().BeTrue();
        }

        [Test]
        public void ErrorIsLoggedOnExceptionWithCorrectException()
        {
            // Arrange
            var autoResetEvent = new AutoResetEvent(false);
            var expectedException = A<Exception>();
            _logMock
                .Setup(m => m.Log(LogLevel.Error, It.IsAny<Func<string>>(), expectedException, It.IsAny<object[]>()))
                .Callback(() => autoResetEvent.Set())
                .Returns((string s, Func<string> f, Exception e, object[] p) => { return true; })
                .Verifiable();

            // Act
            Sut.Run(
                A<Label>(),
                c =>
                    {
                        throw expectedException;
                    },
                CancellationToken.None);

            // Assert
            autoResetEvent.WaitOne(TimeSpan.FromSeconds(10));

            // Assert
            _logMock.Verify();
        }

        [Test]
        public void CancellationTokenIsPassed()
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                // Arrange
                var autoResetEvent = new AutoResetEvent(false);
                var wasCancelled = false;

                // Act
                Sut.Run(
                    A<Label>(),
                    async c =>
                        {
                            try
                            {
                                while (true)
                                {
                                    await Task.Delay(100, c).ConfigureAwait(false);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                wasCancelled = true;
                            }
                            finally
                            {
                                autoResetEvent.Set();
                            }
                        },
                    cancellationTokenSource.Token);

                // Assert
                cancellationTokenSource.Cancel();
                autoResetEvent.WaitOne(TimeSpan.FromSeconds(10));
                wasCancelled.Should().BeTrue();
            }
        }

        [SetUp]
        public void SetUp()
        {
            _logMock = InjectMock<ILog>();
        }
    }
}