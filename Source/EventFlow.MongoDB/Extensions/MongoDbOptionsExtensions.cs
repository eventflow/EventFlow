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
using EventFlow.MongoDB.ReadStores;
using EventFlow.ReadStores;
using EventFlow.MongoDB.EventStore;
using MongoDB.Driver;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventFlow.MongoDB.Extensions
{
    public static class MongoDbOptionsExtensions
    {
        public static IEventFlowOptions ConfigureMongoDb(
            this IEventFlowOptions eventFlowOptions,
            string url,
            string database)
        {
            MongoUrl mongoUrl = new MongoUrl(url);
            var mongoClient = new MongoClient(mongoUrl);
            return eventFlowOptions
                .ConfigureMongoDb(mongoClient, database);
        }

        public static IEventFlowOptions ConfigureMongoDb(
            this IEventFlowOptions eventFlowOptions,
            string database)
        {
            var mongoClient = new MongoClient();
            return eventFlowOptions
                .ConfigureMongoDb(mongoClient, database);
        }

        public static IEventFlowOptions ConfigureMongoDb(
            this IEventFlowOptions eventFlowOptions,
            IMongoClient mongoClient,
            string database)
        {
            IMongoDatabase mongoDatabase = mongoClient.GetDatabase(database);
            return eventFlowOptions.ConfigureMongoDb(() => mongoDatabase);
        }

        public static IEventFlowOptions ConfigureMongoDb(
            this IEventFlowOptions eventFlowOptions,
            Func<IMongoDatabase> mongoDatabaseFactory)
        {
            eventFlowOptions.ServiceCollection.TryAddSingleton(f => mongoDatabaseFactory());
            eventFlowOptions.ServiceCollection.TryAddSingleton<IReadModelDescriptionProvider, ReadModelDescriptionProvider>();
            eventFlowOptions.ServiceCollection.TryAddSingleton<IMongoDbEventSequenceStore, MongoDbEventSequenceStore>();

            return eventFlowOptions;
        }

        public static IEventFlowOptions UseMongoDbReadModel<TReadModel>(
            this IEventFlowOptions eventFlowOptions)
            where TReadModel : class, IMongoDbReadModel
        {
            eventFlowOptions.ServiceCollection.TryAddTransient<IMongoDbReadModelStore<TReadModel>, MongoDbReadModelStore<TReadModel>>();
            eventFlowOptions.ServiceCollection.TryAddTransient<IReadModelStore<TReadModel>>(r => r.GetService<IMongoDbReadModelStore<TReadModel>>());
            
            eventFlowOptions.UseReadStoreFor<IMongoDbReadModelStore<TReadModel>, TReadModel>();
            
            return eventFlowOptions;
        }

        public static IEventFlowOptions UseMongoDbReadModel<TReadModel, TReadModelLocator>(
            this IEventFlowOptions eventFlowOptions)
            where TReadModel : class, IMongoDbReadModel
            where TReadModelLocator : IReadModelLocator
        {
            eventFlowOptions.ServiceCollection.TryAddTransient<IMongoDbReadModelStore<TReadModel>, MongoDbReadModelStore<TReadModel>>();
            eventFlowOptions.ServiceCollection.TryAddTransient<IReadModelStore<TReadModel>>(r => r.GetService<IMongoDbReadModelStore<TReadModel>>());
            
            eventFlowOptions.UseReadStoreFor<IMongoDbReadModelStore<TReadModel>, TReadModel, TReadModelLocator>();
            
            return eventFlowOptions;
        }
    }
}
