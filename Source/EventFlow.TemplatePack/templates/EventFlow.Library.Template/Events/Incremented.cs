using EventFlow.Aggregates;
using EventFlow.EventStores;

namespace EventFlow.Library.Template.Events
{
    [EventVersion(nameof(Incremented), 1)]
    public class Incremented : AggregateEvent<Entity, EntityId> { }
}
