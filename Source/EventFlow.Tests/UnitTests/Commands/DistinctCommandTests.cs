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
using System.Collections.Generic;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Commands
{
    [Category(Categories.Unit)]
    public class DistinctCommandTests
    {
        public class MyDistinctCommand : DistinctCommand<ThingyAggregate, ThingyId, IExecutionResult>
        {
            public int MagicNumber { get; }

            public MyDistinctCommand(
                ThingyId aggregateId,
                int magicNumber) : base(aggregateId)
            {
                MagicNumber = magicNumber;
            }

            protected override IEnumerable<byte[]> GetSourceIdComponents()
            {
                yield return BitConverter.GetBytes(MagicNumber);
                yield return AggregateId.GetBytes();
            }
        }

        [TestCase("thingy-4b1e7b48-18f1-4215-91d9-903cffdab3d8", 1, "command-40867fe4-94da-59e4-9bb5-e532f6565751")]
        [TestCase("thingy-4b1e7b48-18f1-4215-91d9-903cffdab3d8", 2, "command-df0e238e-676b-500e-b962-76fcef97768a")]
        [TestCase("thingy-6a2a04bd-bbc8-44ac-80ac-b0ca56897bc0", 2, "command-0455a861-bc9e-56c5-b7b9-5c14671db8b2")]
        public void Arguments(string aggregateId, int magicNumber, string expectedSouceId)
        {
            // Arrange
            var testId = ThingyId.With(aggregateId);
            var command = new MyDistinctCommand(testId, magicNumber);

            // Act
            var sourceId = command.SourceId;

            // Assert
            sourceId.Value.Should().Be(expectedSouceId);
        }
    }
}
