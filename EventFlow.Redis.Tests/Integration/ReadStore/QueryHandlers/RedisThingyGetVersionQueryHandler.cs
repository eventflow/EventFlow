using EventFlow.Queries;
using EventFlow.Redis.ReadStore;
using EventFlow.Redis.Tests.Integration.ReadStore.ReadModels;
using EventFlow.TestHelpers.Aggregates.Queries;
using Redis.OM;

namespace EventFlow.Redis.Tests.Integration.ReadStore.QueryHandlers;

public class RedisThingyGetVersionQueryHandler : RedisQueryHandler<RedisThingyReadModel>,
    IQueryHandler<ThingyGetVersionQuery, long?>
{
    public RedisThingyGetVersionQueryHandler(RedisConnectionProvider provider) : base(provider)
    {
    }

    public async Task<long?> ExecuteQueryAsync(ThingyGetVersionQuery query, CancellationToken cancellationToken)
    {
        var thingy = await Collection.FindByIdAsync(query.ThingyId.Value);
        return thingy?.Version;
    }
}