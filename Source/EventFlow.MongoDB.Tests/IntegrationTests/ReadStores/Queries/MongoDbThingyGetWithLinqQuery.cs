using System.Linq;
using EventFlow.MongoDB.Tests.IntegrationTests.ReadStores.ReadModels;
using EventFlow.Queries;
using EventFlow.TestHelpers.Aggregates;

namespace EventFlow.MongoDB.Tests.IntegrationTests.ReadStores.Queries
{
    public class MongoDbThingyGetWithLinqQuery : IQuery<IQueryable<MongoDbThingyReadModel>>
    {
        public MongoDbThingyGetWithLinqQuery()
        {
        }
    }
}