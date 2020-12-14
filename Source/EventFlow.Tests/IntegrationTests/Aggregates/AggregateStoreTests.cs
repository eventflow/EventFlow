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
using EventFlow.Configuration;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.IntegrationTests.Aggregates
{
    [Category(Categories.Integration)]
    public class AggregateStoreTests : IntegrationTest
    {
        [TestCase(true, 1)]
        [TestCase(false, 0)]
        public async Task ExecutionResultShouldControlEventStore(bool isSuccess, int expectedAggregateVersion)
        {
            // Arrange
            var pingId = PingId.New;
            var thingyId = ThingyId.New;
            
            // Act
            var executionResult = await CommandBus.PublishAsync(
                    new ThingyMaybePingCommand(thingyId, pingId, isSuccess),
                    CancellationToken.None)
                .ConfigureAwait(false);
            executionResult.IsSuccess.Should().Be(isSuccess);

            // Assert
            var thingyAggregate = await AggregateStore.LoadAsync<ThingyAggregate, ThingyId>(
                    thingyId,
                    CancellationToken.None)
                .ConfigureAwait(false);
            thingyAggregate.Version.Should().Be(expectedAggregateVersion);
        }
        
        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions.CreateResolver();
        }
    }
}