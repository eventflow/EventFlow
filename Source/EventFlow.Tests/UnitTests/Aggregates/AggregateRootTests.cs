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

using System.Linq;
using EventFlow.Aggregates;
using EventFlow.Test.Aggregates.Test;
using EventFlow.Test.Aggregates.Test.Events;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Aggregates
{
    [TestFixture]
    public class AggregateRootTests
    {
        private TestAggregate _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = new TestAggregate("42");
        }

        [Test]
        public void InitialVersionIsZero()
        {
            // Assert
            _sut.Version.Should().Be(0);
            _sut.IsNew.Should().BeTrue();
            _sut.UncommittedEvents.Count().Should().Be(0);
        }

        [Test]
        public void ApplyingEventIncrementsVersion()
        {
            // Act
            _sut.Ping();

            // Assert
            _sut.Version.Should().Be(1);
            _sut.IsNew.Should().BeFalse();
            _sut.UncommittedEvents.Count().Should().Be(1);
            _sut.PingsReceived.Should().Be(1);
        }

        [Test]
        public void EventsCanBeApplied()
        {
            // Arrange
            var events = new IAggregateEvent[]
                {
                    new PingEvent(),
                    new PingEvent(),
                };

            // Act
            _sut.ApplyEvents(events);

            // Assert
            _sut.IsNew.Should().BeFalse();
            _sut.Version.Should().Be(2);
            _sut.PingsReceived.Should().Be(2);
            _sut.UncommittedEvents.Count().Should().Be(0);
        }
    }
}
