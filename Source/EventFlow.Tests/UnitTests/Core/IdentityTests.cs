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
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Core
{
    [Category(Categories.Unit)]
    public class IdentityTests
    {
        [Test]
        public void NewDeterministic_ReturnsKnownResult()
        {
            // Arrange
            var namespaceId = Guid.Parse("769077C6-F84D-46E3-AD2E-828A576AAAF3");
            const string name = "fantastic 4";

            // Arrange
            var testId = ThingyId.NewDeterministic(namespaceId, name);

            // Assert
            testId.Value.Should().Be("thingy-da7ab6b1-c513-581f-a1a0-7cdf17109deb");
            ThingyId.IsValid(testId.Value).Should().BeTrue();
        }

        [TestCase("thingy-da7ab6b1-c513-581f-a1a0-7cdf17109deb", "da7ab6b1-c513-581f-a1a0-7cdf17109deb")]
        [TestCase("thingy-00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000000")]
        public void WithValidValue(string value, string expectedGuidValue)
        {
            // Arrange
            ThingyId thingyId = null;
            var expectedGuid = Guid.Parse(expectedGuidValue);

            // Act
            Assert.DoesNotThrow(() => thingyId = ThingyId.With(value));

            // Assert
            thingyId.Should().NotBeNull();
            thingyId.Value.Should().Be(value);
            thingyId.GetGuid().Should().Be(expectedGuid);
        }

        [Test]
        public void InputOutput()
        {
            // Arrange
            var guid = Guid.NewGuid();

            // Act
            var thingyId = ThingyId.With(guid);

            // Assert
            thingyId.GetGuid().Should().Be(guid);
        }

        [Test]
        public void ShouldBeLowerCase()
        {
            // Act
            var testId = ThingyId.New;

            // Assert
            testId.Value.Should().Be(testId.Value.ToLowerInvariant());
        }

        [Test]
        public void New_IsValid()
        {
            // Arrange
            var testId = ThingyId.New;

            // Assert
            ThingyId.IsValid(testId.Value).Should().BeTrue(testId.Value);
        }

        [Test]
        public void NewComb_IsValid()
        {
            // Arrange
            var testId = ThingyId.NewComb();

            // Assert
            ThingyId.IsValid(testId.Value).Should().BeTrue(testId.Value);
        }

        [Test]
        public void NewDeterministic_IsValid()
        {
            // Arrange
            var testId = ThingyId.NewDeterministic(Guid.NewGuid(), Guid.NewGuid().ToString());

            // Assert
            ThingyId.IsValid(testId.Value).Should().BeTrue(testId.Value);
        }

        [TestCase("da7ab6b1-c513-581f-a1a0-7cdf17109deb")]
        [TestCase("thingyid-da7ab6b1-c513-581f-a1a0-7cdf17109deb")]
        [TestCase("thingy-769077C6-F84D-46E3-AD2E-828A576AAAF3")]
        [TestCase("thingy-pppppppp-pppp-pppp-pppp-pppppppppppp")]
        [TestCase("funny-da7ab6b1-c513-581f-a1a0-7cdf17109deb")]
        [TestCase(null)]
        [TestCase("")]
        public void CannotCreateBadIds(string badIdValue)
        {
            // Act
            Assert.Throws<ArgumentException>(() => ThingyId.With(badIdValue)).Message.Should().Contain("Identity is invalid:");
        }
    }
}