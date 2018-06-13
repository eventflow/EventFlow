using EventFlow.Configuration;
using EventFlow.EntityFramework.Extensions;
using EventFlow.TestHelpers;
using NUnit.Framework;

namespace EventFlow.EntityFramework.Tests.InMemory
{
    [Category(Categories.Integration)]
    public class InMemoryEventStoreTests : TestHelpers.Suites.TestSuiteForEventStore
    {
        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions
                .ConfigureEntityFramework()
                .AddDbContextProvider<InMemoryDbContextProvider>()
                .UseEntityFrameworkEventStore<InMemoryDbContextProvider>()
                .CreateResolver();
        }
    }
}
