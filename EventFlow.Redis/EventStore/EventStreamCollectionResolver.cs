using StackExchange.Redis;

namespace EventFlow.Redis.EventStore;

public class EventStreamCollectionResolver : IEventStreamCollectionResolver
{
    private readonly IConnectionMultiplexer _multiplexer;

    public EventStreamCollectionResolver(IConnectionMultiplexer multiplexer)
    {
        _multiplexer = multiplexer;
    }
    
    // Using https://redis.io/commands/scan/ instead of KEYS to reduce blocking on the server.
    public async Task<IEnumerable<PrefixedKey>> GetStreamIdsAsync(CancellationToken cancellationToken = default)
    {
        var cursor = 0;
        var names = new List<PrefixedKey>();
        do
        {
            var result = await _multiplexer.GetDatabase().ExecuteAsync("scan", cursor, "MATCH", $"{Constants.StreamPrefix}*");
            var arr = (RedisResult[]) result;
            cursor = (int) arr[0];
            var prefixedKeys = ((RedisResult[]) arr[1]).Select(n => AsPrefixedKey((string) n));
            names.AddRange(prefixedKeys);
        } while (cursor != 0);

        return names;
    }

    private static PrefixedKey AsPrefixedKey(string k)
    {
        var split = k.Split(':');
        return new PrefixedKey(split[0], split[1]);
    }
}