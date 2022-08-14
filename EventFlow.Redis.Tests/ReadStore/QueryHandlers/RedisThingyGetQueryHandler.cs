using EventFlow.Queries;
using EventFlow.Redis.ReadStore;
using EventFlow.Redis.Tests.ReadStore.ReadModels;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Queries;
using Redis.OM;
using Redis.OM.Searching;

namespace EventFlow.Redis.Tests.ReadStore.QueryHandlers;

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