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

using EventFlow.Extensions;
using EventFlow.TestHelpers;
using EventFlow.Tests.UnitTests.Specifications;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Provided.Specifications
{
    [Category(Categories.Unit)]
    public class AtLeastSpecificationTests
    {
        [TestCase(1, 1, false)]
        [TestCase(1, 2, true)]
        [TestCase(1, 3, true)]
        [TestCase(1, 4, true)]
        [TestCase(1, 5, true)]
        [TestCase(3, 1, false)]
        [TestCase(3, 2, false)]
        [TestCase(3, 3, false)]
        [TestCase(3, 4, true)]
        [TestCase(3, 5, true)]
        public void AtLeast_Returns_Correctly(int requiredSpecifications, int obj, bool expectedIsSatisfiedBy)
        {
            // Arrange
            var isAbove1 = new TestSpecifications.IsAboveSpecification(1);
            var isAbove2 = new TestSpecifications.IsAboveSpecification(2);
            var isAbove3 = new TestSpecifications.IsAboveSpecification(3);
            var isAbove4 = new TestSpecifications.IsAboveSpecification(4);
            var atLeast = new[] { isAbove1, isAbove2, isAbove3, isAbove4 }.AtLeast(requiredSpecifications);

            // Act
            var isSatisfiedBy = atLeast.IsSatisfiedBy(obj);

            // Assert
            isSatisfiedBy.Should().Be(expectedIsSatisfiedBy, string.Join(", ", atLeast.WhyIsNotSatisfiedBy(obj)));
        }
    }
}