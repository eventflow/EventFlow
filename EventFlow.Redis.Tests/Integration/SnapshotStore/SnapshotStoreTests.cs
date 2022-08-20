using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Suites;
using NUnit.Framework;
using StackExchange.Redis;

namespace EventFlow.Redis.Tests.Integration.SnapshotStore;

[Category(Categories.Integration)]
public class SnapshotStoreTests : TestSuiteForSnapshotStore
{
    private readonly TestcontainerDatabase _container
        = new TestcontainersBuilder<RedisTestcontainer>().WithDatabase(
            new RedisTestcontainerConfiguration("redis/redis-stack")
            {
            }).Build();

    protected override IServiceProvider Configure(IEventFlowOptions eventFlowOptions)
    {
        _container.StartAsync().Wait();
        var multiplexer = ConnectionMultiplexer.Connect(_container.ConnectionString);
        eventFlowOptions.ConfigureRedis(multiplexer);
        eventFlowOptions.UseRedisSnapshotStore();

        return base.Configure(eventFlowOptions);
    }
}