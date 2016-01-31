// The MIT License (MIT)
// 
// Copyright (c) 2015-2016 Rasmus Mikkelsen
// Copyright (c) 2015-2016 eBay Software Foundation
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

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.EventStores.MsSql;
using EventFlow.Extensions;
using EventFlow.MsSql.Tests.IntegrationTests.ReadStores.QueryHandlers;
using EventFlow.MsSql.Tests.IntegrationTests.ReadStores.ReadModels;
using EventFlow.ReadStores.MsSql.Extensions;
using EventFlow.Sql;
using EventFlow.Sql.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Suites;
using Helpz.MsSql;
using NUnit.Framework;

namespace EventFlow.MsSql.Tests.IntegrationTests.ReadStores
{
    [Category(Categories.Integration)]
    public class MsSqlReadModelStoreTests : TestSuiteForReadModelStore
    {
        private IMsSqlDatabase _testDatabase;

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            _testDatabase = MsSqlHelpz.CreateDatabase("eventflow");

            var resolver = eventFlowOptions
                .RegisterServices(sr => sr.RegisterType(typeof (ThingyMessageLocator)))
                .ConfigureMsSql(MsSqlConfiguration.New.SetConnectionString(_testDatabase.ConnectionString.Value))
                .UseMssqlReadModel<MsSqlThingyReadModel>()
                .UseMssqlReadModel<MsSqlThingyMessageReadModel, ThingyMessageLocator>()
                .AddQueryHandlers(
                    typeof(MsSqlThingyGetQueryHandler),
                    typeof(MsSqlThingyGetVersionQueryHandler),
                    typeof(MsSqlThingyGetMessagesQueryHandler))
                .CreateResolver();

            var databaseMigrator = resolver.Resolve<ISqlDatabaseMigrator>();
            EventFlowEventStoresMsSql.MigrateDatabase(databaseMigrator);
            databaseMigrator.MigrateDatabaseUsingEmbeddedScripts(GetType().Assembly);

            return resolver;
        }

        protected override Task PurgeTestAggregateReadModelAsync()
        {
            return ReadModelPopulator.PurgeAsync<MsSqlThingyReadModel>(CancellationToken.None);
        }

        protected override Task PopulateTestAggregateReadModelAsync()
        {
            return ReadModelPopulator.PopulateAsync<MsSqlThingyReadModel>(CancellationToken.None);
        }
    }
}