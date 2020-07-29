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

using EventFlow.Exceptions;
using EventFlow.Extensions;
using EventFlow.Specifications;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Specifications
{
    [Category(Categories.Unit)]
    public class SpecificationTests
    {
        [Test]
        public void NotSpecification_ReturnsTrue_ForNotSatisfied()
        {
            // Arrange
            var isTrue = new TestSpecifications.IsTrueSpecification();

            // Act
            var isSatisfiedBy = isTrue.Not().IsSatisfiedBy(false);

            // Act
            isSatisfiedBy.Should().BeTrue();
        }

        [Test]
        public void NotSpeficication_ReturnsFalse_ForSatisfied()
        {
            // Arrange
            var isTrue = new TestSpecifications.IsTrueSpecification();

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
            var leftIsTrue = (ISpecification<bool>) new TestSpecifications.IsTrueSpecification();
            var rightIsTrue = (ISpecification<bool>)new TestSpecifications.IsTrueSpecification();
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
            var leftIsTrue = (ISpecification<bool>)new TestSpecifications.IsTrueSpecification();
            var rightIsTrue = (ISpecification<bool>)new TestSpecifications.IsTrueSpecification();
            if (notLeft) leftIsTrue = leftIsTrue.Not();
            if (notRight) rightIsTrue = rightIsTrue.Not();
            var andSpecification = leftIsTrue.And(rightIsTrue);

            // Act
            var isSatisfiedBy = andSpecification.IsSatisfiedBy(true);

            // Assert
            isSatisfiedBy.Should().Be(expectedResult);
        }

        [Test]
        public void ThrowDomainErrorIfNotStatisfied_Throws_IfNotSatisfied()
        {
            // Arrange
            var isTrue = new TestSpecifications.IsTrueSpecification();

            // Act
            Assert.Throws<DomainError>(() => isTrue.ThrowDomainErrorIfNotSatisfied(false));
        }

        [Test]
        public void ThrowDomainErrorIfNotStatisfied_DoesNotThrow_IfStatisfied()
        {
            // Arrange
            var isTrue = new TestSpecifications.IsTrueSpecification();

            // Act
            Assert.DoesNotThrow(() => isTrue.ThrowDomainErrorIfNotSatisfied(true));
        }
    }
}