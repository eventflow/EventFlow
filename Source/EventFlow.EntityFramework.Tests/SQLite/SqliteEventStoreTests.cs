using EventFlow.Configuration;
using EventFlow.EntityFramework.Extensions;
using EventFlow.TestHelpers;
using NUnit.Framework;

namespace EventFlow.EntityFramework.Tests.SQLite
{
    [Category(Categories.Integration)]
    public class SqliteEventStoreTests : TestHelpers.Suites.TestSuiteForEventStore
    {
        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions
                .ConfigureEntityFramework()
                .AddDbContextProvider<SqliteDbContextProvider>(Lifetime.Singleton)
                .UseEntityFrameworkEventStore<SqliteDbContextProvider>()
                .CreateResolver();
        }
    }
}
