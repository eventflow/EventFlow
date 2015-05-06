// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores.MsSql;
using EventFlow.Extensions;
using EventFlow.MsSql.Extensions;
using EventFlow.MsSql.Tests.Helpers;
using EventFlow.ReadStores.MsSql;
using EventFlow.ReadStores.MsSql.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Test;
using EventFlow.TestHelpers.Aggregates.Test.ReadModels;
using TestAggregateReadModel = EventFlow.MsSql.Tests.ReadModels.TestAggregateReadModel;

namespace EventFlow.MsSql.Tests.IntegrationTests
{
    public class MsSqlIntegrationTestConfiguration : IntegrationTestConfiguration
    {
        protected ITestDatabase TestDatabase { get; private set; }
        protected IMsSqlConnection MsSqlConnection { get; private set; }
        protected IReadModelSqlGenerator ReadModelSqlGenerator { get; private set; }

        public override IRootResolver CreateRootResolver(EventFlowOptions eventFlowOptions)
        {
            TestDatabase = MsSqlHelper.CreateDatabase("eventflow");

            var resolver = eventFlowOptions
                .ConfigureMsSql(MsSqlConfiguration.New.SetConnectionString(TestDatabase.ConnectionString))
                .UseEventStore<MsSqlEventStore>()
                .UseMssqlReadModel<TestAggregate, TestId, TestAggregateReadModel>()
                .CreateResolver();

            MsSqlConnection = resolver.Resolve<IMsSqlConnection>();
            ReadModelSqlGenerator = resolver.Resolve<IReadModelSqlGenerator>();

            var databaseMigrator = resolver.Resolve<IMsSqlDatabaseMigrator>();
            EventFlowEventStoresMsSql.MigrateDatabase(databaseMigrator);
            databaseMigrator.MigrateDatabaseUsingEmbeddedScripts(GetType().Assembly);

            return resolver;
        }

        public override async Task<ITestAggregateReadModel> GetTestAggregateReadModel(IIdentity id)
        {
            var sql = ReadModelSqlGenerator.CreateSelectSql<TestAggregateReadModel>();
            var readModels = await MsSqlConnection.QueryAsync<TestAggregateReadModel>(
                Label.Named("mssql-fetch-test-read-model"), 
                CancellationToken.None,
                sql,
                new { AggregateId = id.Value })
                .ConfigureAwait(false);
            return readModels.SingleOrDefault();
        }

        public override void TearDown()
        {
            TestDatabase.Dispose();
        }
    }
}
