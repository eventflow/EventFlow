using System;
using EventFlow.EventStores;

namespace EventFlow.EntityFramework.EventStores
{
    public class EventEntity : ICommittedDomainEvent
    {
        public long GlobalSequenceNumber { get; set; }
        public Guid BatchId { get; set; }
        public string AggregateId { get; set; }
        public string AggregateName { get; set; }
        public string Data { get; set; }
        public string Metadata { get; set; }
        public int AggregateSequenceNumber { get; set; }
    }
}