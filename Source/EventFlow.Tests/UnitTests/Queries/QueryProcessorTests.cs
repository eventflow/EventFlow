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
using EventFlow.Configuration;
using EventFlow.Core.Caching;
using EventFlow.Logs;
using EventFlow.Queries;
using EventFlow.TestHelpers;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Queries
{
    [Category(Categories.Unit)]
    public class QueryProcessorTests : TestsFor<QueryProcessor>
    {
        private Mock<IResolver> _resolverMock;
        private Mock<IQueryHandler<IQuery<int>, int>> _queryHandlerMock;
    
        public class TestQuery : IQuery<int> { }

        [SetUp]
        public void SetUp()
        {
            Inject<IMemoryCache>(new DictionaryMemoryCache(Mock<ILog>()));

            _resolverMock = InjectMock<IResolver>();
            _queryHandlerMock = new Mock<IQueryHandler<IQuery<int>, int>>();

            _resolverMock
                .Setup(r => r.Resolve(It.Is<Type>(t => t == typeof(IQueryHandler<TestQuery, int>))))
                .Returns(() => _queryHandlerMock.Object);
            _queryHandlerMock
                .Setup(h => h.ExecuteQueryAsync(It.IsAny<IQuery<int>>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(42));
        }

        [Test]
        public async Task QueryHandlerIsInvoked()
        {
            // Act
            var result = await Sut.ProcessAsync(new TestQuery(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            result.Should().Be(42);
            _queryHandlerMock.Verify(q => q.ExecuteQueryAsync(It.IsAny<IQuery<int>>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}