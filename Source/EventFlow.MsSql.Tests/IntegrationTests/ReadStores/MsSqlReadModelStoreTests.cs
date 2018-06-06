// The MIT License (MIT)
// 
// Copyright (c) 2015-2018 Rasmus Mikkelsen
// Copyright (c) 2015-2018 eBay Software Foundation
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
using System.Linq;
using EventFlow.Configuration;
using EventFlow.Extensions;
using EventFlow.MsSql.EventStores;
using EventFlow.MsSql.Extensions;
using EventFlow.MsSql.ReadStores;
using EventFlow.MsSql.Tests.IntegrationTests.ReadStores.QueryHandlers;
using EventFlow.MsSql.Tests.IntegrationTests.ReadStores.ReadModels;
using EventFlow.Sql.Migrations;
using EventFlow.Sql.ReadModels;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.MsSql;
using EventFlow.TestHelpers.Suites;
using NUnit.Framework;

namespace EventFlow.MsSql.Tests.IntegrationTests.ReadStores
{
    [Category(Categories.Integration)]
    public class MsSqlReadModelStoreTests : TestSuiteForReadModelStore
    {
        protected override Type ReadModelType { get; } = typeof(MsSqlThingyReadModel);

        private IMsSqlDatabase _testDatabase;

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            _testDatabase = MsSqlHelpz.CreateDatabase("eventflow");

            var resolver = eventFlowOptions
                .RegisterServices(sr => sr.RegisterType(typeof(ThingyMessageLocator)))
                .ConfigureMsSql(MsSqlConfiguration.New.SetConnectionString(_testDatabase.ConnectionString.Value))
                .UseMssqlReadModel<MsSqlThingyReadModel>()
                .UseMssqlReadModel<MsSqlThingyMessageReadModel, ThingyMessageLocator>()
                .AddQueryHandlers(
                    typeof(MsSqlThingyGetQueryHandler),
                    typeof(MsSqlThingyGetVersionQueryHandler),
                    typeof(MsSqlThingyGetMessagesQueryHandler))
                .CreateResolver();

            // TODO: Cleanup

            var databaseMigrator = resolver.Resolve<IMsSqlDatabaseMigrator>();
            var readModelSchemaGenerator = resolver.Resolve<IMssqlReadModelSchemaGenerator>();
            var readModelAnalyzer = resolver.Resolve<IReadModelAnalyzer>();
            EventFlowEventStoresMsSql.MigrateDatabase(databaseMigrator);

            var sqlScripts = new[] {typeof(MsSqlThingyReadModel), typeof(MsSqlThingyMessageReadModel)}
                .Select(m => new
                    {
                        Schema = readModelSchemaGenerator.GetReadModelSchema(m),
                        Details = readModelAnalyzer.GetReadModelDetails(m)
                    })
                .Select(a => new SqlScript(a.Details.TableName, a.Schema));

            // TODO: Missing index IX_ReadModel-ThingyMessage_ThingyId

            databaseMigrator.MigrateDatabaseUsingScripts(sqlScripts);

            return resolver;
        }

        [TearDown]
        public void TearDown()
        {
            _testDatabase.DisposeSafe("Failed to delete database");
        }
    }
}