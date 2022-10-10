using StackExchange.Redis;

namespace EventFlow.Redis.ReadStore;

internal static class Extensions
{
    internal static IEnumerable<HashEntry> ToHashEntries(this IReadOnlyDictionary<string, string> dict)
    {
        return dict.ToArray().Select(kv => new HashEntry(kv.Key, kv.Value));
    }
}