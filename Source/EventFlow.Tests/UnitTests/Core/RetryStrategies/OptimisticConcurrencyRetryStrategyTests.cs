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
using EventFlow.Core.RetryStrategies;
using EventFlow.Exceptions;
using EventFlow.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Core.RetryStrategies
{
    [Category(Categories.Unit)]
    public class OptimisticConcurrencyRetryStrategyTests : TestsFor<OptimisticConcurrencyRetryStrategy>
    {
        private EventFlowOptions _eventFlowOptions;
        private Mock<IOptions<EventFlowOptions>> _eventFlowConfigurationMock;

        [SetUp]
        public void SetUp()
        {
            _eventFlowOptions = new EventFlowOptions
                {
                    NumberOfRetriesOnOptimisticConcurrencyExceptions = 3,
                    DelayBeforeRetryOnOptimisticConcurrencyExceptions = TimeSpan.FromMilliseconds(10),
                };
            _eventFlowConfigurationMock = InjectMock<IOptions<EventFlowOptions>>();
            _eventFlowConfigurationMock
                .Setup(m => m.Value)
                .Returns(() => _eventFlowOptions);
        }

        [TestCase(0, true)]
        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(3, true)]
        [TestCase(4, false)]
        public void ShouldThisBeRetried_OptimisticConcurrencyException_ShouldBeRetired(int currentRetryCount, bool expectedShouldThisBeRetried)
        {
            // Assert
            var optimisticConcurrencyException = new OptimisticConcurrencyException(A<string>());

            // Act
            var shouldThisBeRetried = Sut.ShouldThisBeRetried(optimisticConcurrencyException, A<TimeSpan>(), currentRetryCount);

            // Assert
            shouldThisBeRetried.ShouldBeRetried.Should().Be(expectedShouldThisBeRetried);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void ShouldThisBeRetried_Exception_ShouldNeverBeRetired(int currentRetryCount)
        {
            // Assert
            var exception = A<Exception>();

            // Act
            var shouldThisBeRetried = Sut.ShouldThisBeRetried(exception, A<TimeSpan>(), currentRetryCount);

            // Assert
            shouldThisBeRetried.ShouldBeRetried.Should().BeFalse();
        }

    }
}
