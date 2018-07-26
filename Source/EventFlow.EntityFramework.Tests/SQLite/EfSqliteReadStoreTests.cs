using System;
using EventFlow.Configuration;
using EventFlow.EntityFramework.Extensions;
using EventFlow.EntityFramework.Tests.Model;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Suites;
using NUnit.Framework;

namespace EventFlow.EntityFramework.Tests.SQLite
{
    [Category(Categories.Integration)]
    public class EfSqliteReadStoreTests : TestSuiteForReadModelStore
    {
        protected override Type ReadModelType => typeof(ThingyReadModelEntity);

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions
                .ConfigureEntityFramework()
                .ConfigureForReadStoreTest<SqliteDbContextProvider>()
                .CreateResolver();
        }
    }
}
