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

using EventFlow.Extensions;
using EventFlow.ReadStores;
using EventFlow.Redis.EventStore;
using EventFlow.Redis.ReadStore;
using EventFlow.Redis.SnapshotStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Redis.OM;
using StackExchange.Redis;

namespace EventFlow.Redis;

public static class Extensions
{
    /// <summary>
    /// Adds the required services to use redis with EventFlow
    /// </summary>
    /// <param name="options"></param>
    /// <param name="multiplexer">A Multiplexer connected to a redis database</param>
    /// <returns></returns>
    public static IEventFlowOptions ConfigureRedis(this IEventFlowOptions options, IConnectionMultiplexer multiplexer)
    {
        var provider = new RedisConnectionProvider(multiplexer);
        options.ServiceCollection.TryAddSingleton(multiplexer);
        options.ServiceCollection.TryAddSingleton(provider);

        return options;
    }

    /// <summary>
    /// Adds the required services to use redis with EventFlow
    /// </summary>
    /// <param name="options"></param>
    /// <param name="connectionString">The connection string to connect with redis</param>
    /// <returns></returns>
    public static IEventFlowOptions ConfigureRedis(this IEventFlowOptions options, string connectionString)
    {
        var multiplexer = ConnectionMultiplexer.Connect(connectionString);
        var provider = new RedisConnectionProvider(multiplexer);
        options.ServiceCollection.TryAddSingleton(multiplexer);
        options.ServiceCollection.TryAddSingleton(provider);

        return options;
    }

    /// <summary>
    /// Configures Redis as the event persistence. Requires Redis >= v5.0
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IEventFlowOptions UseRedisEventStore(this IEventFlowOptions options)
    {
        options.ServiceCollection.TryAddTransient<IEventStreamCollectionResolver, EventStreamCollectionResolver>();
        options.UseEventPersistence<RedisEventPersistence>();

        return options;
    }

    /// <summary>
    /// Configures Redis as the read model store for the given read model. Requires Redis >= 5.0 and the redis search extension
    /// </summary>
    /// <param name="options"></param>
    /// <typeparam name="TReadModel">The type of the redis ReadModel. Can be queried by using the IndexedAttribute from <a href="https://github.com/redis/redis-om-dotnet"> Redis.Om</a> on the required properties and inheriting from <see cref="RedisQueryHandler{TReadModel}"/></typeparam>
    /// <returns></returns>
    public static IEventFlowOptions UseRedisReadModel<TReadModel>(this IEventFlowOptions options)
        where TReadModel : RedisReadModel
    {
        options.ServiceCollection.TryAddTransient<IRedisHashBuilder, RedisHashBuilder>();
        options.ServiceCollection.TryAddTransient<IReadModelStore<TReadModel>, RedisReadModelStore<TReadModel>>();

        options.UseReadStoreFor<IReadModelStore<TReadModel>, TReadModel>();

        var provider = options.ServiceCollection.BuildServiceProvider().GetRequiredService<RedisConnectionProvider>();
        provider.Connection.CreateIndex(typeof(TReadModel));

        return options;
    }

    /// <summary>
    /// Configures Redis as the read model store for the given read model. Requires Redis >= 5.0 and the redis search extension
    /// </summary>
    /// <param name="options"></param>
    /// <typeparam name="TReadModel">The type of the redis ReadModel. Can be queried by using the IndexedAttribute from <a href="https://github.com/redis/redis-om-dotnet"> Redis.Om</a> on the required properties and inheriting from <see cref="RedisQueryHandler{TReadModel}"/></typeparam>
    /// <typeparam name="TReadModelLocator">The type of the ReadModelLocator</typeparam>
    /// <returns></returns>
    public static IEventFlowOptions UseRedisReadModel<TReadModel, TReadModelLocator>(this IEventFlowOptions options)
        where TReadModel : RedisReadModel where TReadModelLocator : IReadModelLocator
    {
        options.ServiceCollection.TryAddTransient<IRedisHashBuilder, RedisHashBuilder>();
        options.ServiceCollection.TryAddTransient<IReadModelStore<TReadModel>, RedisReadModelStore<TReadModel>>();

        options.UseReadStoreFor<IReadModelStore<TReadModel>, TReadModel, TReadModelLocator>();

        var provider = options.ServiceCollection.BuildServiceProvider().GetRequiredService<RedisConnectionProvider>();
        provider.Connection.CreateIndex(typeof(TReadModel));

        return options;
    }

    /// <summary>
    /// Configures Redis as a snapshot store for EventFlow. Requires Redis >= v5.0 and the Redis Json extension
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IEventFlowOptions UseRedisSnapshotStore(this IEventFlowOptions options)
    {
        options.UseSnapshotPersistence<RedisSnapshotPersistence>(ServiceLifetime.Transient);
        var provider = options.ServiceCollection.BuildServiceProvider().GetRequiredService<RedisConnectionProvider>();
        provider.Connection.CreateIndex(typeof(RedisSnapshot));

        return options;
    }
}