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
using EventFlow.Queries;
using EventFlow.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Queries
{
    [TestFixture]
    [Category(Categories.Unit)]
    public class SerializedQueryProcessorTests: TestsFor<SerializedQueryProcessor>
    {
        private Mock<IJsonSerializer> _jsonSerializer;
        private Mock<IQueryDefinitionService> _queryDefinitionService;
        private Mock<IQueryProcessor> _queryProcesor;

        public class TestQuery : IQuery<int> { }

        [SetUp]
        public void SetUp()
        {
            _jsonSerializer = InjectMock<IJsonSerializer>();
            InjectMock<ILogger<SerializedQueryProcessor>>();
            _queryDefinitionService = InjectMock<IQueryDefinitionService>();
            _queryProcesor = InjectMock<IQueryProcessor>();

        }

        [Test]
        public async Task ProcessAsync_NoName_ThrowsArgumentNullException()
        {
            Func<Task> action = () => Sut.ProcessAsync(null, 1, "json");
            await action.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [Test]
        public async Task ProcessAsync_VersionNegative_ThrowsArgumentOutOfRangeException()
        {
            Func<Task> action = () => Sut.ProcessAsync("TestQuery", -1, "json");
            await action.Should().ThrowAsync<ArgumentOutOfRangeException>().ConfigureAwait(false);
        }

        [Test]
        public async Task ProcessAsync_JsonNull_ThrowsArgumentNullException()
        {
            Func<Task> action = () => Sut.ProcessAsync("TestQuery", 1, null);
            await action.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [Test]
        public async Task ProcessAsync_QueryDefinitionDoesNotExist_ThrowsArgumentException()
        {
            const string queryName = "TestQuery";
            const int version = 1;
            const string json = "fake-json";
            QueryDefinition queryDefinition = null;
            _queryDefinitionService.Setup(x => x.TryGetDefinition(queryName, version, out queryDefinition)).Returns(false);

            Func<Task> action = () => Sut.ProcessAsync(queryName, version, json);

            await action.Should().ThrowAsync<ArgumentException>();

        }

        [Test]
        public async Task ProcessAsync_JsonSerializationFails_ThrowsArgumentException()
        {
            const string queryName = "TestQuery";
            const int version = 1;
            const string json = "fake-json";
            var queryDefinition = new QueryDefinition(1, typeof(TestQuery), "TestQuery");
            _queryDefinitionService.Setup(x => x.TryGetDefinition(queryName, version, out queryDefinition)).Returns(true);

            _jsonSerializer.Setup(x => x.Deserialize(json, queryDefinition.Type)).Throws<JsonSerializationException>();
            Func<Task> action = () => Sut.ProcessAsync(queryName, version, json);

            await action.Should().ThrowAsync<ArgumentException>();
        }

        [Test]
        public async Task ProcessAsync_ReturnsQueryResults()
        {
            const string queryName = "TestQuery";
            const int version = 1;
            const string json = "fake-json";
            var queryDefinition = new QueryDefinition(1, typeof(TestQuery), "TestQuery");
            _queryDefinitionService.Setup(x => x.TryGetDefinition(queryName, version, out queryDefinition)).Returns(true);

            IQuery serializationResults = new TestQuery();
            _jsonSerializer.Setup(x => x.Deserialize(json, queryDefinition.Type)).Returns(serializationResults);

            const int expectedValue = 42;
            _queryProcesor.Setup(x => x.ProcessAsync(serializationResults, CancellationToken.None)).Returns(Task.FromResult<object>(expectedValue));

            var actualResults = await Sut.ProcessAsync(queryName, version, json);

            actualResults.Should().Be(expectedValue);
        }
    }
}
