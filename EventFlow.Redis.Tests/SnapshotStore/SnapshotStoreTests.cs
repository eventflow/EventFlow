using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using EventFlow.Extensions;
using EventFlow.ReadStores;
using EventFlow.Redis.ReadStore;
using EventFlow.Redis.SnapshotStore;
using EventFlow.Redis.Tests.ReadStore.ReadModels;
using EventFlow.TestHelpers.Suites;
using Microsoft.Extensions.DependencyInjection;
using Redis.OM;
using StackExchange.Redis;

namespace EventFlow.Redis.Tests.SnapshotStore;

public class SnapshotStoreTests : TestSuiteForSnapshotStore
{
    private readonly TestcontainerDatabase _container
        = new TestcontainersBuilder<RedisTestcontainer>().WithDatabase(new RedisTestcontainerConfiguration("redis/redis-stack")
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