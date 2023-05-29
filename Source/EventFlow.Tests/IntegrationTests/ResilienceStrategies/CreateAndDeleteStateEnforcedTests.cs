// The MIT License (MIT)
// 
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
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
using EventFlow.EventStores;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.ResilienceStrategies;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using EventFlow.TestHelpers.Aggregates.ValueObjects;

namespace EventFlow.Tests.IntegrationTests.ResilienceStrategies
{
    [Category(Categories.Integration)]
    public class CreateAndDeleteStateEnforcedTests : IntegrationTest
    {

        [OneTimeSetUp]
        public void SetDefaults()
        {
            InjectedServices = (collection) => collection.AddTransient<IAggregateStoreResilienceStrategy, CreateAndDeleteStateEnforcedResilienceStrategy>();
        }

        [Test]
        public async Task InitiateCommandShouldSucceedWhenEmpty()
        {
            // Arrange
            var thingyId = ThingyId.New;
            var expectedAggregateVersion = 1;

            // Act
            var executionResult = await CommandBus.PublishAsync(
                    new ThingyInitiateCommand(thingyId),
                    CancellationToken.None)
                .ConfigureAwait(false);
            executionResult.IsSuccess.Should().BeTrue();

            // Assert
            var thingyAggregate = await AggregateStore.LoadAsync<ThingyAggregate, ThingyId>(
                    thingyId,
                    CancellationToken.None)
                .ConfigureAwait(false);
            thingyAggregate.Version.Should().Be(expectedAggregateVersion);
        }

        [Test]
        public void NonInitiateCommandShouldFailWhenEmpty()
        {
            // Arrange
            var thingyId = ThingyId.New;
            var pingId = PingId.New;

            // Act
            Assert.ThrowsAsync<InvalidOperationException>(async () => await CommandBus.PublishAsync(
                    new ThingyPingCommand(thingyId, pingId),
                    CancellationToken.None));
        }

        [Test]
        public async Task InitiateCommandShouldFailWhenNotEmpty()
        {
            // Arrange
            var thingyId = ThingyId.New;
            await CommandBus.PublishAsync(
                    new ThingyInitiateCommand(thingyId),
                    CancellationToken.None)
                .ConfigureAwait(false);

            // Act
            Assert.ThrowsAsync<InvalidOperationException>(async () => await CommandBus.PublishAsync(
                    new ThingyInitiateCommand(thingyId),
                    CancellationToken.None));
        }

        [Test]
        public async Task DeleteCommandShouldChangeState()
        {
            // Arrange
            var thingyId = ThingyId.New;
            await CommandBus.PublishAsync(
                    new ThingyInitiateCommand(thingyId),
                    CancellationToken.None)
                .ConfigureAwait(false);

            // Act
            var executionResult = await CommandBus.PublishAsync(
                    new ThingyDeleteCommand(thingyId),
                    CancellationToken.None)
                .ConfigureAwait(false);

            executionResult.IsSuccess.Should().BeTrue();

            // Assert
            var thingyAggregate = await AggregateStore.LoadAsync<ThingyAggregate, ThingyId>(
                    thingyId,
                    CancellationToken.None)
                .ConfigureAwait(false);
            thingyAggregate.IsDeleted.Should().BeTrue();
        }

        [Test]
        public async Task AnyCommandShouldFailWhenDeleted()
        {
            // Arrange
            var thingyId = ThingyId.New;
            var pingId = PingId.New;

            await CommandBus.PublishAsync(
                    new ThingyInitiateCommand(thingyId),
                    CancellationToken.None)
                .ConfigureAwait(false);

            await CommandBus.PublishAsync(
                    new ThingyDeleteCommand(thingyId),
                    CancellationToken.None)
                .ConfigureAwait(false);

            // Act
            Assert.ThrowsAsync<InvalidOperationException>(async () => await CommandBus.PublishAsync(
                    new ThingyPingCommand(thingyId, pingId),
                    CancellationToken.None));
        }
    }
}