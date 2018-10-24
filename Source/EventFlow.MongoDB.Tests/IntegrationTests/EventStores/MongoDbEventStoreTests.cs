using EventFlow.Configuration;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Suites;
using EventFlow.Extensions;
using EventFlow.MongoDB.EventStore;
using EventFlow.MongoDB.Extensions;
using NUnit.Framework;
using Mongo2Go;

namespace EventFlow.MongoDB.Tests.IntegrationTests.EventStores
{
	[Category(Categories.Integration)]
	[TestFixture]
	[NUnit.Framework.Timeout(30000)]
    public class MongoDbEventStoreTests : TestSuiteForEventStore
	{
		private MongoDbRunner _runner;
		
		protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
		{
		    _runner = MongoDbRunner.Start();
            var resolver = eventFlowOptions
				.ConfigureMongoDb(_runner.ConnectionString, "eventflow")
				.UseEventStore<MongoDbEventPersistence>()
				.CreateResolver();
			
			return resolver;
		}

		[TearDown]
		public void TearDown()
		{
			_runner.Dispose();
		}
	}
}
