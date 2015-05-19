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

using System.Linq;
using EventFlow.EventStores;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.EventStores
{
    [Timeout(5000)]
    public class GlobalSequenceNumberRangeTests : Test
    {
        [TestCase(1, 1, 1, 1)]
        [TestCase(1, 2, 1, 2)]
        [TestCase(1, 3, 2, 2)]
        [TestCase(2, 3, 1, 2)]
        [TestCase(3, 8, 3, 2)]
        [TestCase(8, 8, 8, 1)]
        public void BatchesAreCorrect(long from, long to, long batchSize, int expectedNumberOfBatches)
        {
            // Act
            var batches = GlobalSequenceNumberRange.Batches(from, to, batchSize).ToList();

            // Assert
            batches.Count.Should().Be(expectedNumberOfBatches);
            batches.Min(r => r.From).Should().Be(from);
            batches.Max(r => r.To).Should().Be(to);

            batches.Zip(batches.Skip(1), (r1, r2) => r2.From - r1.To).Sum().Should().Be(expectedNumberOfBatches - 1);
        }
    }
}
