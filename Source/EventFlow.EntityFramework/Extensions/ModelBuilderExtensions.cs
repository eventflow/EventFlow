using EventFlow.EntityFramework.EventStores;
using EventFlow.EntityFramework.SnapshotStores;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework.Extensions
{
    public static class ModelBuilderExtensions
    {
        public static ModelBuilder AddEventFlowEvents(this ModelBuilder modelBuilder)
        {
            var eventEntity = modelBuilder.Entity<EventEntity>();
            eventEntity.HasKey(e => e.GlobalSequenceNumber);
            eventEntity.HasIndex(e => new {e.AggregateId, e.AggregateSequenceNumber}).IsUnique();
            return modelBuilder;
        }

        public static ModelBuilder AddEventFlowSnapshots(this ModelBuilder modelBuilder)
        {
            var eventEntity = modelBuilder.Entity<SnapshotEntity>();
            eventEntity.HasKey(e => e.Id);
            eventEntity.HasIndex(e => new {e.AggregateName, e.AggregateId, e.AggregateSequenceNumber}).IsUnique();
            return modelBuilder;
        }
    }
}
