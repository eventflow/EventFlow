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

        [TestCase("test-4b1e7b48-18f1-4215-91d9-903cffdab3d8", 42, "command-e466c887-98a6-5a2d-bf75-61a616b7817f")]
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
