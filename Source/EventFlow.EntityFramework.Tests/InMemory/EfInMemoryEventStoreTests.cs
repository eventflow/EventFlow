using EventFlow.Configuration;
using EventFlow.EntityFramework.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Suites;
using NUnit.Framework;

namespace EventFlow.EntityFramework.Tests.InMemory
{
    [Category(Categories.Integration)]
    public class EfInMemoryEventStoreTests : TestSuiteForEventStore
    {
        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions
                .ConfigureEntityFramework()
                .ConfigureForEventStoreTest<InMemoryDbContextProvider>()
                .CreateResolver();
        }
    }
}
