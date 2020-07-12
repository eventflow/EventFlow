// The MIT License (MIT)
// 
// Copyright (c) 2015 Rasmus Mikkelsen
// Copyright (c) 2015 eBay Software Foundation
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using EventFlow.Core;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Core
{
    [Obsolete("AsyncHelper should be removed in 1.0")]
    [Category(Categories.Integration)]
    public class AsyncHelperTests
    {
        [Test]
        public void EmptyIsExpectedToFinish()
        {
            using (AsyncHelper.Wait)
            {
                // Left empty
            }
        }

        [Test, Description("Have a look at ReferenceDeadlockImplementation1 and ReferenceDeadlockImplementation2")]
        public void DoesNotDeadlock()
        {
            // Arrange
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
            string result = null;

            // Act
            using (var a = AsyncHelper.Wait)
            {
                a.Run(PotentialDeadlockAsync("no deadlock"), r => result = r);
            }

            // Assert
            // Expected to actually finish
            result.Should().Be("no deadlock");
        }

        [Test]
        public void ThrowsAggregateExceptionForTwoExceptions()
        {
            // Act + Assert
            Assert.Throws<AggregateException>(() =>
            {
                using (var a = AsyncHelper.Wait)
                {
                    a.Run(Task.WhenAll(ThrowsTestExceptionAsync(), ThrowsTestExceptionAsync()));
                }
            });
        }

        [Test]
        public void ThrowsAggregateExceptionForOneException()
        {
            // Act + Assert
            Assert.Throws<AggregateException>(() =>
            {
                using (var a = AsyncHelper.Wait)
                {
                    a.Run(ThrowsTestExceptionAsync());
                }
            });
        }

        [Test]
        public void AsyncOperationIsAllowedToFinish()
        {
            // Arrange
            var wasExecuted = false;

            // Act
            using (var a = AsyncHelper.Wait)
            {
                a.Run(Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(0.5)).ConfigureAwait(true);
                        wasExecuted = true;
                    }));
            }

            // Assert
            wasExecuted.Should().BeTrue();
        }

        [Test]
        public void NestedAsyncOperationsAreAllowedToFinish()
        {
            // Arrange
            var rootWasExecuted = false;
            var levelsWereExecuted = new bool[10];

            // Act
            using (var a = AsyncHelper.Wait)
            {
                a.Run(Enumerable.Range(0, 10).Aggregate(
                    Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(true);
                        rootWasExecuted = true;
                        Console.WriteLine("Root task done");
                    }),
                    (t, i) => Task.Run(async () =>
                    {
                        await t.ConfigureAwait(true);
                        await Task.Delay(TimeSpan.FromSeconds(0.1)).ConfigureAwait(true);
                        Console.WriteLine($"Task level {i} done");
                        levelsWereExecuted[i] = true;
                    })));
            }

            // Assert
            rootWasExecuted.Should().BeTrue();
            levelsWereExecuted.All(b => b).Should().BeTrue();
        }

        [Test, Explicit("For reference: Will deadlock!")]
        public async Task ReferenceDeadlockImplementation1()
        {
            // Arrange
            var synchronizationContext = new DispatcherSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);

            // Act
            var result = await PotentialDeadlockAsync("deadlock");

            // Assert
            // Will NOT be thrown
            throw new Exception(result);
        }

        [Test, Explicit("For reference: Will deadlock!")]
        public async Task ReferenceDeadlockImplementation2()
        {
            // Arrange
            var synchronizationContext = new DispatcherSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);

            // Act
            var result = await PotentialDeadlockAsync("deadlock");

            // Assert
            // Will NOT be thrown
            throw new Exception(result);
        }

        private static async Task ThrowsTestExceptionAsync()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10)).ConfigureAwait(true);
            throw new TestException();
        }

        private static async Task<string> PotentialDeadlockAsync(string returnValue)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(true);
            return returnValue;
        }

        public class TestException : Exception
        {
        }
    }
}