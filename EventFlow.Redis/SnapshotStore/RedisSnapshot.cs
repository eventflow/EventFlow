using System.Runtime.InteropServices;
using Redis.OM.Modeling;

namespace EventFlow.Redis.SnapshotStore;

//Storage type json is required due to https://github.com/redis/redis-om-dotnet/issues/175
[Document(StorageType = StorageType.Json,Prefixes = new []{Constants.SnapshotPrefix})]
public class RedisSnapshot
{
    [RedisIdField] public string Id { get; set; }
    public long? Version { get; set; }
    [Indexed] public string AggregateId { get; set; }
    [Indexed] public string AggregateName { get; set; }
    [Indexed] public int AggregateSequenceNumber { get; set; }
    public string Data { get; set; }
    public string Metadata { get; set; }
}