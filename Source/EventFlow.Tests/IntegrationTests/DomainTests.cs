// The MIT License (MIT)
//
// Copyright (c) 2015 EventFlow
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

using System;
using EventFlow.Configuration;
using EventFlow.EventStores;
using EventFlow.Tests.TestAggregates;
using EventFlow.Tests.TestAggregates.Commands;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.IntegrationTests
{
    [TestFixture]
    public class DomainTests
    {
        [Test]
        public void BasicFlow()
        {
            // Arrange
            var options = new EventFlowOptions()
                .AddEvents(typeof(TestAggregate).Assembly);
            var resolve = options.CreateResolver();
            var commandBus = resolve.Resolve<ICommandBus>();
            var eventStore = resolve.Resolve<IEventStore>();
            var id = Guid.NewGuid().ToString();

            // Act
            commandBus.Publish(new TestACommand(id));
            var testAggregate = eventStore.LoadAggregate<TestAggregate>(id);

            // Assert
            testAggregate.TestAReceived.Should().BeTrue();
        }
    }
}
