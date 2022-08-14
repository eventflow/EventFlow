using EventFlow.Queries;
using EventFlow.Redis.ReadStore;
using EventFlow.Redis.Tests.ReadStore.ReadModels;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Aggregates.Queries;
using Redis.OM;

namespace EventFlow.Redis.Tests.ReadStore.QueryHandlers;

public class RedisThingyGetMessagesQueryHandler: RedisQueryHandler<RedisThingyMessageReadModel>,  IQueryHandler<ThingyGetMessagesQuery, IReadOnlyCollection<ThingyMessage>>
{
    public RedisThingyGetMessagesQueryHandler(RedisConnectionProvider provider) : base(provider)
    {
    }

    public async Task<IReadOnlyCollection<ThingyMessage>> ExecuteQueryAsync(ThingyGetMessagesQuery query, CancellationToken cancellationToken)
    {
        var thingyId = query.ThingyId.Value;
        var messages = await Collection.Where(rm => rm.ThingyId == thingyId).ToListAsync();

        return messages.Select(r => r.ToThingyMessage()).ToArray();
    }
}