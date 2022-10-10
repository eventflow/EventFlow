using EventFlow.Queries;
using EventFlow.Redis.ReadStore;
using EventFlow.Redis.Tests.Integration.ReadStore.ReadModels;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Queries;
using Redis.OM;

namespace EventFlow.Redis.Tests.Integration.ReadStore.QueryHandlers;

public class RedisThingyGetQueryHandler : RedisQueryHandler<RedisThingyReadModel>, IQueryHandler<ThingyGetQuery, Thingy>
{
    public RedisThingyGetQueryHandler(RedisConnectionProvider provider) : base(provider)
    {
    }

    public async Task<Thingy> ExecuteQueryAsync(ThingyGetQuery query, CancellationToken cancellationToken)
    {
        return (await Collection.FindByIdAsync(query.ThingyId.Value))?.ToThingy();
    }
}