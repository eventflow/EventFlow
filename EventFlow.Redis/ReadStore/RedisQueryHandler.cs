using Redis.OM;
using Redis.OM.Searching;

namespace EventFlow.Redis.ReadStore;

public abstract class RedisQueryHandler<TReadModel> where TReadModel: RedisReadModel
{
    protected readonly IRedisCollection<TReadModel> Collection;

    protected RedisQueryHandler(RedisConnectionProvider provider)
    {
        Collection = provider.RedisCollection<TReadModel>();
    }
}