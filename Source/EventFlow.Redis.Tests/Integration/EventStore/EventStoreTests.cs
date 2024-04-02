// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using EventFlow.Core;
using EventFlow.Redis.EventStore;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using EventFlow.TestHelpers.Suites;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using StackExchange.Redis;

namespace EventFlow.Redis.Tests.Integration.EventStore;

[NUnit.Framework.Category(Categories.Integration)]
public class EventStoreTests : TestSuiteForEventStore
{
    private readonly TestcontainerDatabase _container
        = new TestcontainersBuilder<RedisTestcontainer>().WithDatabase(new RedisTestcontainerConfiguration
        {
        }).Build();

    protected override IServiceProvider Configure(IEventFlowOptions eventFlowOptions)
    {
        _container.StartAsync().Wait();
        var multiplexer = ConnectionMultiplexer.Connect(_container.ConnectionString);
        eventFlowOptions.ConfigureRedis(multiplexer);
        eventFlowOptions.UseRedisEventStore();

        return base.Configure(eventFlowOptions);
    }

    [Test]
    public async Task StreamResolverReturnsAllAggregateIds()
    {
        //Arrange
        var resolver = ServiceProvider.GetRequiredService<IEventStreamCollectionResolver>();
        var firstId = ThingyId.New;
        var secondId = ThingyId.New;
        var firstAggregate = await LoadAggregateAsync(firstId).ConfigureAwait(false);
        firstAggregate.Ping(PingId.New);
        var secondAggregate = await LoadAggregateAsync(secondId).ConfigureAwait(false);
        secondAggregate.Ping(PingId.New);
        await firstAggregate.CommitAsync(EventStore, SnapshotStore, SourceId.New, CancellationToken.None)
            .ConfigureAwait(false);
        await secondAggregate.CommitAsync(EventStore, SnapshotStore, SourceId.New, CancellationToken.None)
            .ConfigureAwait(false);

        //Act
        var keys = await resolver.GetStreamIdsAsync();
        var names = keys.Select(k => k.Key);

        //Assert
        names.Should().Contain(firstId.Value);
        names.Should().Contain(secondId.Value);
    }
}