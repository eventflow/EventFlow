using StackExchange.Redis;

namespace EventFlow.Redis.ReadStore;

public static class Extensions
{
    public static IEnumerable<HashEntry> ToHashEntries(this Dictionary<string, string> dict)
    {
        return dict.ToArray().Select(kv => new HashEntry(kv.Key, kv.Value));
    }
}