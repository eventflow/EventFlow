using EventFlow.Configuration;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Suites;
using NUnit.Framework;

namespace EventFlow.EntityFramework.Tests.InMemory
{
    [Category(Categories.Integration)]
    public class EfInMemorySnapshotTests : TestSuiteForSnapshotStore
    {
        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions
                .ConfigureForSnapshotStoreTest<InMemoryDbContextProvider>()
                .CreateResolver();
        }
    }
}
