using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using EventFlow.Extensions;
using EventFlow.Redis.EventStore;
using EventFlow.TestHelpers.Suites;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using StackExchange.Redis;
using Xunit;

namespace EventFlow.Redis.Tests.EventStore;

public class EventStoreTests : TestSuiteForEventStore
{
    private readonly TestcontainerDatabase _container
        = new TestcontainersBuilder<RedisTestcontainer>().WithDatabase(new RedisTestcontainerConfiguration
        {
        }).WithWaitStrategy(Wait.ForUnixContainer()).Build();

    protected override IServiceProvider Configure(IEventFlowOptions eventFlowOptions)
    {
        _container.StartAsync().Wait();
        var multiplexer = ConnectionMultiplexer.Connect("localhost:6379");
        eventFlowOptions.ServiceCollection.AddSingleton<IConnectionMultiplexer>(multiplexer);
        eventFlowOptions.ServiceCollection
            .AddTransient<IEventStreamCollectionResolver, EventStreamCollectionResolver>();
        eventFlowOptions.UseEventPersistence<RedisEventPersistence>();
        
        return base.Configure(eventFlowOptions);
    }

    [Test]
    public void a()
    {
        Console.WriteLine();
    }
    
    
}