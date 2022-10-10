using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using EventFlow.Extensions;
using EventFlow.Redis.Tests.Integration.ReadStore.QueryHandlers;
using EventFlow.Redis.Tests.Integration.ReadStore.ReadModels;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Suites;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using StackExchange.Redis;

namespace EventFlow.Redis.Tests.Integration.ReadStore;

[Category(Categories.Integration)]
public class RedisReadStoreTests : TestSuiteForReadModelStore
{
    private readonly TestcontainerDatabase _container
        = new TestcontainersBuilder<RedisTestcontainer>().WithDatabase(
            new RedisTestcontainerConfiguration("redis/redis-stack")
            {
            }).Build();

    protected override Type ReadModelType => typeof(RedisThingyReadModel);

    protected override IServiceProvider Configure(IEventFlowOptions eventFlowOptions)
    {
        _container.StartAsync().Wait();
        var multiplexer = ConnectionMultiplexer.Connect(_container.ConnectionString);
        eventFlowOptions.ConfigureRedis(multiplexer);
        eventFlowOptions.ServiceCollection.AddTransient<ThingyMessageLocator>();
        eventFlowOptions.UseRedisReadModel<RedisThingyReadModel>();
        eventFlowOptions.UseRedisReadModel<RedisThingyMessageReadModel, ThingyMessageLocator>();

        eventFlowOptions.AddQueryHandlers(typeof(RedisThingyGetQueryHandler),
            typeof(RedisThingyGetMessagesQueryHandler), typeof(RedisThingyGetVersionQueryHandler));

        return base.Configure(eventFlowOptions);
    }
}