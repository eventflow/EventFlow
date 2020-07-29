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

using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Aggregates
{
    [Category(Categories.Unit)]
    public class AggregateIdTests
    {
        [Test]
        public void ManuallyCreatedIsOk()
        {
            // Arrange
            const string value = "thingy-d15b1562-11f2-4645-8b1a-f8b946b566d3";

            // Act
            var testId = ThingyId.With(value);

            // Test
            testId.Value.Should().Be(value);
        }

        [Test]
        public void CreatedIsDifferent()
        {
            // Act
            var id1 = ThingyId.New;
            var id2 = ThingyId.New;

            // Assert
            id1.Value.Should().NotBe(id2.Value);
        }

        [Test]
        public void SameIdsAreEqual()
        {
            // Arrange
            const string value = "thingy-d15b1562-11f2-4645-8b1a-f8b946b566d3";
            var id1 = ThingyId.With(value);
            var id2 = ThingyId.With(value);

            // Assert
            id1.Equals(id2).Should().BeTrue();
            (id1 == id2).Should().BeTrue();
        }

        [Test]
        public void DifferentAreNotEqual()
        {
            // Arrange
            var id1 = ThingyId.With("thingy-7ddc487f-02ad-4be3-a6ef-71203d333c61");
            var id2 = ThingyId.With("thingy-d15b1562-11f2-4645-8b1a-f8b946b566d3");

            // Assert
            id1.Equals(id2).Should().BeFalse();
            (id1 == id2).Should().BeFalse();
        }
    }
}