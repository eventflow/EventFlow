using EventFlow.EntityFramework.EventStores;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework
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
    }
}