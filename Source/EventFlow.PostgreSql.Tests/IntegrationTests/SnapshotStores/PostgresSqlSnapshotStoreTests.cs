﻿// The MIT License (MIT)
// 
// Copyright (c) 2015-2018 Rida Messaoudene
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
using EventFlow.PostgreSql.Connections;
using EventFlow.PostgreSql.Extensions;
using EventFlow.PostgreSql.SnapshotStores;
using EventFlow.PostgreSql.TestsHelpers;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Suites;

using NUnit.Framework;

namespace EventFlow.PostgreSql.Tests.IntegrationTests.SnapshotStores
{
    [Category(Categories.Integration)]
    public class PostgreSqlSnapshotStoreTests : TestSuiteForSnapshotStore
    {
        private IPostgreSqlDatabase _testDatabase;

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            _testDatabase = PostgreSqlHelpz.CreateDatabase("eventflow-snapshots");

            var resolver = eventFlowOptions
                .ConfigurePostgreSql(PostgreSqlConfiguration.New.SetConnectionString(_testDatabase.ConnectionString.Value))
                .UsePostgreSqlSnapshotStore()
                .CreateResolver();

            var databaseMigrator = resolver.Resolve<IPostgreSqlDatabaseMigrator>();
            EventFlowSnapshotStoresPostgreSql.MigrateDatabase(databaseMigrator);

            return resolver;
        }

        [TearDown]
        public void TearDown()
        {
            _testDatabase.DisposeSafe("Failed to delete database");
        }
    }
}