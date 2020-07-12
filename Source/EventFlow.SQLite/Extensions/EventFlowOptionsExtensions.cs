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

using System;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.ReadStores;
using EventFlow.Sql.ReadModels;
using EventFlow.SQLite.Connections;
using EventFlow.SQLite.EventStores;
using EventFlow.SQLite.ReadStores;
using EventFlow.SQLite.RetryStrategies;

namespace EventFlow.SQLite.Extensions
{
    public static class EventFlowOptionsExtensions
    {
        public static IEventFlowOptions ConfigureSQLite(
            this IEventFlowOptions eventFlowOptions,
            ISQLiteConfiguration sqLiteConfiguration)
        {
            return eventFlowOptions
                .RegisterServices(f =>
                {
                    f.Register<ISQLiteConnection, SQLiteConnection>();
                    f.Register<ISQLiteConnectionFactory, SQLiteConnectionFactory>();
                    f.Register<ISQLiteErrorRetryStrategy, SQLiteErrorRetryStrategy>();
                    f.Register(_ => sqLiteConfiguration, Lifetime.Singleton);
                });
        }

        public static IEventFlowOptions UseSQLiteEventStore(
            this IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions
                .UseEventStore<SQLiteEventPersistence>();
        }

        public static IEventFlowOptions UseSQLiteReadModel<TReadModel, TReadModelLocator>(
            this IEventFlowOptions eventFlowOptions)
            where TReadModel : class, IReadModel
            where TReadModelLocator : IReadModelLocator
        {
            return eventFlowOptions
                .RegisterServices(RegisterSQLiteReadStore<TReadModel>)
                .UseReadStoreFor<ISQLiteReadModelStore<TReadModel>, TReadModel, TReadModelLocator>();
        }

        public static IEventFlowOptions UseSQLiteReadModel<TReadModel>(
            this IEventFlowOptions eventFlowOptions)
            where TReadModel : class, IReadModel
        {
            return eventFlowOptions
                .RegisterServices(RegisterSQLiteReadStore<TReadModel>)
                .UseReadStoreFor<ISQLiteReadModelStore<TReadModel>, TReadModel>();
        }

        [Obsolete("Use the simpler method UseSQLiteReadModel<TReadModel> instead.")]
        public static IEventFlowOptions UseSQLiteReadModelFor<TAggregate, TIdentity, TReadModel>(
            this IEventFlowOptions eventFlowOptions)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TReadModel : class, IReadModel
        {
            return eventFlowOptions
                .RegisterServices(RegisterSQLiteReadStore<TReadModel>)
                .UseReadStoreFor<TAggregate, TIdentity, ISQLiteReadModelStore<TReadModel>, TReadModel>();
        }

        private static void RegisterSQLiteReadStore<TReadModel>(
            IServiceRegistration serviceRegistration)
            where TReadModel : class, IReadModel
        {
            serviceRegistration.Register<IReadModelSqlGenerator, ReadModelSqlGenerator>(Lifetime.Singleton, true);
            serviceRegistration.Register<ISQLiteReadModelStore<TReadModel>, SQLiteReadModelStore<TReadModel>>();
            serviceRegistration.Register<IReadModelStore<TReadModel>>(r => r.Resolver.Resolve<ISQLiteReadModelStore<TReadModel>>());
        }
    }
}