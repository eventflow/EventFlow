﻿// The MIT License (MIT)
// 
// Copyright (c) 2015 Rasmus Mikkelsen
// Copyright (c) 2015 eBay Software Foundation
// https://github.com/rasmus/EventFlow
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
// 

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores.MsSql;
using EventFlow.Extensions;
using EventFlow.MsSql.Extensions;
using EventFlow.MsSql.Tests.IntegrationTests.QueryHandlers;
using EventFlow.MsSql.Tests.ReadModels;
using EventFlow.Queries;
using EventFlow.ReadStores;
using EventFlow.ReadStores.MsSql;
using EventFlow.ReadStores.MsSql.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Test.Entities;
using EventFlow.TestHelpers.Aggregates.Test.Queries;
using EventFlow.TestHelpers.Aggregates.Test.ReadModels;
using Helpz.MsSql;

namespace EventFlow.MsSql.Tests.IntegrationTests
{
    public class MsSqlIntegrationTestConfiguration : IntegrationTestConfiguration
    {
        protected IMsSqlDatabase TestDatabase { get; private set; }
        protected IMsSqlConnection MsSqlConnection { get; private set; }
        protected IReadModelSqlGenerator ReadModelSqlGenerator { get; private set; }
        protected IReadModelPopulator ReadModelPopulator { get; private set; }

        public override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            TestDatabase = MsSqlHelpz.CreateDatabase("eventflow");

            var resolver = eventFlowOptions
                .RegisterServices(sr =>
                    {
                        sr.RegisterType(typeof (MsSqlTestItemLocator));
                        sr.Register<IQueryHandler<GetItemsQuery, IReadOnlyCollection<TestItem>>, MsSqlGetItemsQueryHandler>();
                    })
                .ConfigureMsSql(MsSqlConfiguration.New.SetConnectionString(TestDatabase.ConnectionString.Value))
                .UseEventStore<MsSqlEventPersistence>()
                .UseMssqlReadModel<MsSqlTestAggregateReadModel>()
                .UseMssqlReadModel<MsSqlTestAggregateItemReadModel, MsSqlTestItemLocator>()
                .CreateResolver();

            MsSqlConnection = resolver.Resolve<IMsSqlConnection>();
            ReadModelSqlGenerator = resolver.Resolve<IReadModelSqlGenerator>();
            ReadModelPopulator = resolver.Resolve<IReadModelPopulator>();

            var databaseMigrator = resolver.Resolve<IMsSqlDatabaseMigrator>();
            EventFlowEventStoresMsSql.MigrateDatabase(databaseMigrator);
            databaseMigrator.MigrateDatabaseUsingEmbeddedScripts(GetType().Assembly);

            return resolver;
        }

        public override async Task<ITestAggregateReadModel> GetTestAggregateReadModelAsync(IIdentity id)
        {
            var sql = ReadModelSqlGenerator.CreateSelectSql<MsSqlTestAggregateReadModel>();
            var readModels = await MsSqlConnection.QueryAsync<MsSqlTestAggregateReadModel>(
                Label.Named("mssql-fetch-test-read-model"), 
                CancellationToken.None,
                sql,
                new { AggregateId = id.Value })
                .ConfigureAwait(false);
            return readModels.SingleOrDefault();
        }

        public override Task PurgeTestAggregateReadModelAsync()
        {
            return ReadModelPopulator.PurgeAsync<MsSqlTestAggregateReadModel>(CancellationToken.None);
        }

        public override Task PopulateTestAggregateReadModelAsync()
        {
            return ReadModelPopulator.PopulateAsync<MsSqlTestAggregateReadModel>(CancellationToken.None);
        }

        public override void TearDown()
        {
            TestDatabase.Dispose();
        }
    }
}
