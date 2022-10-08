namespace EventFlow.Redis.ReadStore;

internal interface IRedisHashBuilder
{
    /// <summary>
    /// Builds a Dictionary out of the provided object by using reflection
    /// </summary>
    /// <param name="obj">The object</param>
    /// <returns>A Dictionary containing all properties, indexed by their property name</returns>
    IReadOnlyDictionary<string, string> BuildHashSet(object obj);
}