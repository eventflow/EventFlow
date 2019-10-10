using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.MongoDB.ReadStores;
using EventFlow.MongoDB.Tests.IntegrationTests.ReadStores.Queries;
using EventFlow.MongoDB.Tests.IntegrationTests.ReadStores.ReadModels;
using EventFlow.Queries;
using EventFlow.TestHelpers.Aggregates;

namespace EventFlow.MongoDB.Tests.IntegrationTests.ReadStores.QueryHandlers
{
    public class MongoDbThingyGetWithLinqQueryHandler : IQueryHandler<MongoDbThingyGetWithLinqQuery, IQueryable<MongoDbThingyReadModel>>
    {

        private readonly IMongoDbReadModelStore<MongoDbThingyReadModel> _readStore;
        public MongoDbThingyGetWithLinqQueryHandler(
            IMongoDbReadModelStore<MongoDbThingyReadModel> mongeReadStore)
        {
            _readStore = mongeReadStore;
        }

        public Task<IQueryable<MongoDbThingyReadModel>> ExecuteQueryAsync(MongoDbThingyGetWithLinqQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(_readStore.AsQueryable());
        }
    }
}