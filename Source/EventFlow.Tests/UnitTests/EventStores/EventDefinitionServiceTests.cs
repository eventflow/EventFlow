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

using System;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using EventFlow.Logs;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.EventStores
{
    [TestFixture]
    public class EventDefinitionServiceTests
    {
        [EventVersion("Fancy", 42)]
        public class TestEventWithLongName : AggregateEvent<IAggregateRoot> { }
        public class TestEvent : AggregateEvent<IAggregateRoot> { }
        public class TestEventV2 : AggregateEvent<IAggregateRoot> { }
        public class OldTestEventV5 : AggregateEvent<IAggregateRoot> { }

        private EventDefinitionService _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = new EventDefinitionService(new Mock<ILog>().Object);
        }

        [TestCase(typeof(TestEvent), 1, "TestEvent")]
        [TestCase(typeof(TestEventV2), 2, "TestEvent")]
        [TestCase(typeof(OldTestEventV5), 5, "TestEvent")]
        [TestCase(typeof(TestEventWithLongName), 42, "Fancy")]
        public void GetEventDefinition_EventWithVersion(Type eventType, int expectedVersion, string expectedName)
        {
            // Act
            var eventDefinition = _sut.GetEventDefinition(eventType);

            // Assert
            eventDefinition.Name.Should().Be(expectedName);
            eventDefinition.Version.Should().Be(expectedVersion);
            eventDefinition.Type.Should().Be(eventType);
        }

        [TestCase("TestEvent", 1, typeof(TestEvent))]
        [TestCase("TestEvent", 2, typeof(TestEventV2))]
        [TestCase("TestEvent", 5, typeof(OldTestEventV5))]
        [TestCase("Fancy", 42, typeof(TestEventWithLongName))]
        public void LoadEventsFollowedByGetEventDefinition_ReturnsCorrectAnswer(string eventName, int eventVersion, Type expectedEventType)
        {
            // Arrange
            _sut.LoadEvents(new []
                {
                    typeof(TestEvent),
                    typeof(TestEventV2),
                    typeof(OldTestEventV5),
                    typeof(TestEventWithLongName)
                });

            // Act
            var eventDefinition = _sut.GetEventDefinition(eventName, eventVersion);

            // Assert
            eventDefinition.Name.Should().Be(eventName);
            eventDefinition.Version.Should().Be(eventVersion);
            eventDefinition.Type.Should().Be(expectedEventType);
        }
    }
}
