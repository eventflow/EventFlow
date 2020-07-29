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

using EventFlow.Configuration;
using EventFlow.Extensions;
using EventFlow.MongoDB.ReadStores;
using EventFlow.ReadStores;
using EventFlow.MongoDB.EventStore;
using MongoDB.Driver;
using System;

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
            return eventFlowOptions.RegisterServices(sr =>
            {
                sr.Register(f => mongoDatabaseFactory(), Lifetime.Singleton);
                sr.Register<IReadModelDescriptionProvider, ReadModelDescriptionProvider>(Lifetime.Singleton, true);
                sr.Register<IMongoDbEventSequenceStore, MongoDbEventSequenceStore>(Lifetime.Singleton);
            });
        }

        public static IEventFlowOptions UseMongoDbReadModel<TReadModel>(
            this IEventFlowOptions eventFlowOptions)
            where TReadModel : class, IMongoDbReadModel
        {
            return eventFlowOptions
                .RegisterServices(f =>
                {
                    f.Register<IMongoDbReadModelStore<TReadModel>, MongoDbReadModelStore<TReadModel>>();
                    f.Register<IReadModelStore<TReadModel>>(r => r.Resolver.Resolve<IMongoDbReadModelStore<TReadModel>>());
                })
                .UseReadStoreFor<IMongoDbReadModelStore<TReadModel>, TReadModel>();
        }

        public static IEventFlowOptions UseMongoDbReadModel<TReadModel, TReadModelLocator>(
            this IEventFlowOptions eventFlowOptions)
            where TReadModel : class, IMongoDbReadModel
            where TReadModelLocator : IReadModelLocator
        {
            return eventFlowOptions
                .RegisterServices(f =>
                {
                    f.Register<IMongoDbReadModelStore<TReadModel>, MongoDbReadModelStore<TReadModel>>();
                    f.Register<IReadModelStore<TReadModel>>(r => r.Resolver.Resolve<IMongoDbReadModelStore<TReadModel>>());
                })
                .UseReadStoreFor<IMongoDbReadModelStore<TReadModel>, TReadModel, TReadModelLocator>();
        }
    }
}
