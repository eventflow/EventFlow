using System;
using System.Threading.Tasks;
using EventFlow.Extensions;
using EventFlow.ReadStores;
using EventFlow.Strategies;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Suites;
using EventFlow.Tests.IntegrationTests.ReadStores.QueryHandlers;
using EventFlow.Tests.IntegrationTests.ReadStores.ReadModels;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EventFlow.Tests.IntegrationTests.Strategies
{
    [Category(Categories.Integration)]
    public class ReadStoreCachingStrategyTests : TestSuiteForReadModelStore
    {
        protected override Type ReadModelType { get; } = typeof(InMemoryThingyReadModel);

        protected override IServiceProvider Configure(IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions
                .RegisterServices(sr => {
                    sr.AddTransient<IReadStoreCachingStrategy, InMemoryReadStoreCachingStrategy>();
                    sr.AddTransient<IReadStoreCachingConfiguration, ReadStoreCachingConfiguration>();
                    sr.AddTransient(typeof(ThingyMessageLocator));
                    })
                .UseInMemoryReadStoreFor<InMemoryThingyReadModel>()
                .UseInMemoryReadStoreFor<InMemoryThingyMessageReadModel, ThingyMessageLocator>()
                .AddQueryHandlers(
                    typeof(InMemoryThingyGetQueryHandler),
                    typeof(InMemoryThingyGetVersionQueryHandler),
                    typeof(InMemoryThingyGetMessagesQueryHandler))
                .ServiceCollection.BuildServiceProvider();
        }

        [Test]
        public override Task OptimisticConcurrencyCheck()
        {
            // The in-memory uses a global lock on all read models making concurrency impossible
            return Task.FromResult(0);
        }
    }
}
