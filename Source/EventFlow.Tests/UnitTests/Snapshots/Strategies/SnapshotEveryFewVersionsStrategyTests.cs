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

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Snapshots;
using EventFlow.Snapshots.Strategies;
using EventFlow.TestHelpers;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Snapshots.Strategies
{
    [Category(Categories.Unit)]
    public class SnapshotEveryFewVersionsStrategyTests
    {
        [TestCase(0, null, false)]
        [TestCase(99, null, false)]
        [TestCase(100, null, true)]
        [TestCase(101, null, true)]
        [TestCase(99, 100, false)] // Can't happen, but tested
        [TestCase(100, 100, false)]
        [TestCase(101, 100, false)]
        [TestCase(200, 100, true)]
        [TestCase(201, 100, true)]
        [TestCase(1000, 100, true)]
        public async Task ShouldCreateSnapshotAsync_ReturnsCorrect(int aggregateRootVersion, int? snapshotVersion, bool expectedShouldCreateSnapshot)
        {
            // Assumptions
            SnapshotEveryFewVersionsStrategy.DefautSnapshotAfterVersions.Should().Be(100);

            // Arrange
            var sut = SnapshotEveryFewVersionsStrategy.Default;
            var snapshotAggregateRootMock = new Mock<ISnapshotAggregateRoot>();
            snapshotAggregateRootMock.Setup(a => a.Version).Returns(aggregateRootVersion);
            snapshotAggregateRootMock.Setup(a => a.SnapshotVersion).Returns(snapshotVersion);

            // Act
            var shouldCreateSnapshot = await sut.ShouldCreateSnapshotAsync(
                snapshotAggregateRootMock.Object,
                CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            shouldCreateSnapshot.Should().Be(expectedShouldCreateSnapshot);
        }
    }
}