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
using EventFlow.TestHelpers.Aggregates.Test;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Core
{
    public class IdentityTests
    {
        [Test]
        public void NewDeterministic()
        {
            // Arrange
            var namespaceId = Guid.Parse("769077C6-F84D-46E3-AD2E-828A576AAAF3");
            const string name = "fantastic 4";

            // Arrange
            var testId = TestId.NewDeterministic(namespaceId, name);

            // Assert
            testId.Value.Should().Be("test-da7ab6b1-c513-581f-a1a0-7cdf17109deb");
            TestId.IsValid(testId.Value).Should().BeTrue();
        }

        [Test]
        public void WithValidValue()
        {
            Assert.DoesNotThrow(() => TestId.With("test-da7ab6b1-c513-581f-a1a0-7cdf17109deb"));
        }

        [Test]
        public void ShouldBeLowerCase()
        {
            // Act
            var testId = TestId.New;

            // Assert
            testId.Value.Should().Be(testId.Value.ToLowerInvariant());
        }

        [Test]
        public void NewIsValid()
        {
            // Arrange
            var testId = TestId.New;

            // Assert
            TestId.IsValid(testId.Value).Should().BeTrue();
        }

        [TestCase("da7ab6b1-c513-581f-a1a0-7cdf17109deb")]
        [TestCase("test-769077C6-F84D-46E3-AD2E-828A576AAAF3")]
        [TestCase(null)]
        [TestCase("")]
        public void CannotCreateBadIds(string badIdValue)
        {
            // Act
            Assert.Throws<ArgumentException>(() => TestId.With(badIdValue));
        }
    }
}
