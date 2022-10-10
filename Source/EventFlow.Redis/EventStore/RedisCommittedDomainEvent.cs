using EventFlow.EventStores;

namespace EventFlow.Redis.EventStore;

internal class RedisCommittedDomainEvent : ICommittedDomainEvent
{
    public RedisCommittedDomainEvent(string aggregateId, string data, string metadata, int aggregateSequenceNumber)
    {
        AggregateId = aggregateId;
        Data = data;
        Metadata = metadata;
        AggregateSequenceNumber = aggregateSequenceNumber;
    }

    public string AggregateId { get; init; }
    public string Data { get; init; }
    public string Metadata { get; init; }
    public int AggregateSequenceNumber { get; init; }
}