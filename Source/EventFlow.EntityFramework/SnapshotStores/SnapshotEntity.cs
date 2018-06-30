namespace EventFlow.EntityFramework.SnapshotStores
{
    public class SnapshotEntity
    {
        public long Id { get; set; }
        public string AggregateId { get; set; }
        public string AggregateName { get; set; }
        public int AggregateSequenceNumber { get; set; }
        public string Data { get; set; }
        public string Metadata { get; set; }
    }
}