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
using EventFlow.Commands;
using EventFlow.TestHelpers.Aggregates.Test;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Commands
{
    [Timeout(10000)]
    public class DeterministicIdCommandTests
    {
        public class MyDeterministicIdCommand : DeterministicIdCommand<TestAggregate, TestId>
        {
            public int MagicNumber { get; }

            public MyDeterministicIdCommand(
                TestId aggregateId,
                int magicNumber) : base(aggregateId)
            {
                MagicNumber = magicNumber;
            }

            protected override IEnumerable<object> GetEqualityComponents()
            {
                yield return MagicNumber;
                yield return AggregateId;
            }
        }

        [TestCase("test-4b1e7b48-18f1-4215-91d9-903cffdab3d8", 1, "command-9cb313f1-1887-5abf-931f-71b54ac11d18")]
        [TestCase("test-4b1e7b48-18f1-4215-91d9-903cffdab3d8", 2, "command-7cd1f748-cb72-5f1b-9336-90388fdfda8a")]
        [TestCase("test-6a2a04bd-bbc8-44ac-80ac-b0ca56897bc0", 2, "command-3f985e40-79af-5fe7-8d0e-c87d5bc334f6")]
        public void Arguments(string aggregateId, int magicNumber, string expectedSouceId)
        {
            // Arrange
            var testId = TestId.With(aggregateId);
            var command = new MyDeterministicIdCommand(testId, magicNumber);

            // Act
            var sourceId = command.SourceId;

            // Assert
            sourceId.Value.Should().Be(expectedSouceId);
        }
    }
}
