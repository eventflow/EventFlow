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

using System.IO;
using System.Threading;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.EventStores.Files;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Test;
using EventFlow.TestHelpers.Aggregates.Test.Commands;
using EventFlow.TestHelpers.Aggregates.Test.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.IntegrationTests
{
    public class BackwardCompatibilityTests : Test
    {
        private readonly TestId _testId = TestId.With("test-1acea1eb-3e11-45c0-83c1-bc32e57ee8e7");
        private IResolver _resolver;
        private ICommandBus _commandBus;
        private IEventStore _eventStore;

        [SetUp]
        public void SetUp()
        {

            var codeBase = ReflectionHelper.GetCodeBase(GetType().Assembly);
            var filesEventStoreDirectory = Path.GetFullPath(Path.Combine(codeBase, "..", "..", "TestData", "FilesEventStore"));

            _resolver = EventFlowOptions.New
                .UseFilesEventStore(FilesEventStoreConfiguration.Create(filesEventStoreDirectory))
                .AddEvents(EventFlowTestHelpers.Assembly)
                .AddCommandHandlers(EventFlowTestHelpers.Assembly)
                .CreateResolver();

            _commandBus = _resolver.Resolve<ICommandBus>();
            _eventStore = _resolver.Resolve<IEventStore>();
        }

        [Test]
        public void ValidateTestAggregate()
        {
            // Act
            var testAggregate = _eventStore.LoadAggregate<TestAggregate, TestId>(_testId, CancellationToken.None);

            // Assert
            testAggregate.Version.Should().Be(2);
            testAggregate.PingsReceived.Should().Contain(PingId.With("95433aa0-11f7-4128-bd5f-18e0ecc4d7c1"));
            testAggregate.PingsReceived.Should().Contain(PingId.With("2352d09b-4712-48cc-bb4f-5560d7c52558"));
        }

        [Test, Explicit]
        public void CreateEventHelper()
        {
            _commandBus.Publish(new PingCommand(_testId, PingId.New), CancellationToken.None);
        }
    }
}
