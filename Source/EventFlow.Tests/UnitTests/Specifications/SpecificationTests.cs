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

using System.Collections.Generic;
using EventFlow.Extensions;
using EventFlow.Specifications;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Specifications
{
    public class SpecificationTests
    {
        [Test]
        public void ShouldReturnTrueForSatisfiedValue()
        {
            // Arrange
            var isTrue = new IsTrueSpecification();

            // Act
            var isSatisfiedBy = isTrue.IsSatisfiedBy(true);

            // Act
            isSatisfiedBy.Should().BeTrue();
        }

        [Test]
        public void ShouldReturnFalseForNotSatisfiedValue()
        {
            // Arrange
            var isTrue = new IsTrueSpecification();

            // Act
            var isSatisfiedBy = isTrue.IsSatisfiedBy(false);

            // Act
            isSatisfiedBy.Should().BeFalse();
        }

        [Test]
        public void NotSpeficication_ReturnsTrue_ForNotSatisfied()
        {
            // Arrange
            var isTrue = new IsTrueSpecification();

            // Act
            var isSatisfiedBy = isTrue.Not().IsSatisfiedBy(false);

            // Act
            isSatisfiedBy.Should().BeTrue();
        }

        [Test]
        public void NotSpeficication_ReturnsFalse_ForSatisfied()
        {
            // Arrange
            var isTrue = new IsTrueSpecification();

            // Act
            var isSatisfiedBy = isTrue.Not().IsSatisfiedBy(true);

            // Act
            isSatisfiedBy.Should().BeFalse();
        }

        [TestCase(true, true, false)]
        [TestCase(true, false, true)]
        [TestCase(false, true, true)]
        [TestCase(false, false, true)]
        public void OrSpeficication_ReturnsTrue_Correctly(bool notLeft, bool notRight, bool expectedResult)
        {
            // Arrange
            var leftIsTrue = (ISpecification<bool>) new IsTrueSpecification();
            var rightIsTrue = (ISpecification<bool>)new IsTrueSpecification();
            if (notLeft) leftIsTrue = leftIsTrue.Not();
            if (notRight) rightIsTrue = rightIsTrue.Not();
            var orSpecification = leftIsTrue.Or(rightIsTrue);

            // Act
            var isSatisfiedBy = orSpecification.IsSatisfiedBy(true);

            // Assert
            isSatisfiedBy.Should().Be(expectedResult);
        }

        [TestCase(true, true, false)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(false, false, true)]
        public void AndSpeficication_ReturnsTrue_Correctly(bool notLeft, bool notRight, bool expectedResult)
        {
            // Arrange
            var leftIsTrue = (ISpecification<bool>)new IsTrueSpecification();
            var rightIsTrue = (ISpecification<bool>)new IsTrueSpecification();
            if (notLeft) leftIsTrue = leftIsTrue.Not();
            if (notRight) rightIsTrue = rightIsTrue.Not();
            var andSpecification = leftIsTrue.And(rightIsTrue);

            // Act
            var isSatisfiedBy = andSpecification.IsSatisfiedBy(true);

            // Assert
            isSatisfiedBy.Should().Be(expectedResult);
        }

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
            var isAbove1 = new IsAboveSpecification(1);
            var isAbove2 = new IsAboveSpecification(2);
            var isAbove3 = new IsAboveSpecification(3);
            var isAbove4 = new IsAboveSpecification(4);
            var atLeast = new[]
                {
                    isAbove1,
                    isAbove2,
                    isAbove3,
                    isAbove4
                }
                .AtLeast(requiredSpecifications);

            // Act
            var isSatisfiedBy = atLeast.IsSatisfiedBy(obj);

            // Assert
            isSatisfiedBy.Should().Be(expectedIsSatisfiedBy, string.Join(", ", atLeast.WhyIsNotStatisfiedBy(obj)));
        }

        [TestCase(4, 3, false)]
        [TestCase(4, 4, false)]
        [TestCase(4, 5, true)]
        public void IsAbove_Returns_Correct(int limit, int obj, bool expectedIsSatisfiedBy)
        {
            // Arrange
            var isAbove = new IsAboveSpecification(limit);

            // Act
            var isSatisfiedBy = isAbove.IsSatisfiedBy(obj);

            // Assert
            isSatisfiedBy.Should().Be(expectedIsSatisfiedBy);
        }

        public class IsAboveSpecification : Specification<int>
        {
            private readonly int _limit;

            public IsAboveSpecification(
                int limit)
            {
                _limit = limit;
            }

            protected override IEnumerable<string> IsNotStatisfiedBecause(int obj)
            {
                if (obj <= _limit)
                {
                    yield return $"{obj} is less or equal than {_limit}";
                }
            }
        }

        public class IsTrueSpecification : Specification<bool>
        {
            protected override IEnumerable<string> IsNotStatisfiedBecause(bool obj)
            {
                if (!obj)
                {
                    yield return "Its false!";
                }
            }
        }
    }
}