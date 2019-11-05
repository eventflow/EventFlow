namespace EventFlow.EventStores.StreamsDb
{
    public class StreamsDbEvent : ICommittedDomainEvent
    {
        public string AggregateId { get; set; }
        public string Data { get; set; }
        public string Metadata { get; set; }
        public int AggregateSequenceNumber { get; set; }
    }
}