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
using EventFlow.Core.Caching;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventFlow.TestHelpers.Suites
{
    public abstract class TestSuiteForInMemoryCache<TSut> : TestsFor<TSut>
        where TSut : IMemoryCache
    {
        [Test]
        public async Task InvokesFactoryAndReturnsValue()
        {
            // Arrange
            var value = A<object>();
            var factory = CreateFactoryMethod(value);

            // Act
            var cacheValue = await Sut.GetOrAddAsync(
                A<CacheKey>(),
                DateTimeOffset.Now.AddDays(1),
                factory.Object,
                CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            cacheValue.Should().BeSameAs(value);
            factory.Verify(m => m(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Test]
        public void FaultyFactoryMethodThrowsException()
        {
            // Arrange
            var exception = A<Exception>();
            var faultyFactory = CreateFaultyFactoryMethod<object>(exception);

            // Act
            var thrownException = Assert.ThrowsAsync<Exception>(async () => await Sut.GetOrAddAsync(
                A<CacheKey>(),
                DateTimeOffset.Now.AddDays(1),
                faultyFactory.Object,
                CancellationToken.None));

            // Assert
            faultyFactory.Verify(m => m(It.IsAny<CancellationToken>()), Times.Once());
            thrownException.Should().BeSameAs(exception);
        }

        [Test]
        public void FactoryReturningNullThrowsException()
        {
            // Arrange
            var factory = CreateFactoryMethod<object>(null);

            // Act
            var thrownException = Assert.ThrowsAsync<InvalidOperationException>(async () => await Sut.GetOrAddAsync(
                A<CacheKey>(),
                DateTimeOffset.Now.AddDays(1),
                factory.Object,
                CancellationToken.None));

            // Assert
            thrownException.Message.Should().Contain("must not return 'null");
        }

        private static Mock<Func<CancellationToken, Task<T>>> CreateFactoryMethod<T>(T value)
        {
            var mock = new Mock<Func<CancellationToken, Task<T>>>();
            mock
                .Setup(m => m(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(value));
            return mock;
        }

        private static Mock<Func<CancellationToken, Task<T>>> CreateFaultyFactoryMethod<T>(Exception exception)
        {
            var mock = new Mock<Func<CancellationToken, Task<T>>>();
            mock
                .Setup(m => m(It.IsAny<CancellationToken>()))
                .Returns(() => { throw exception; });
            return mock;
        }
    }
}