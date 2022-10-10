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