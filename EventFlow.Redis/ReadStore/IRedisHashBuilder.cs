using StackExchange.Redis;

namespace EventFlow.Redis.ReadStore;

public interface IRedisHashBuilder
{
    /// <summary>
    /// Build a Dictionary<string, string> out of the provided object by using reflection
    /// </summary>
    /// <param name="obj">The object</param>
    /// <returns>A Dictionary containing all properties, indexed by their property name</returns>
    Dictionary<string, string> BuildHashSet(object obj);
}