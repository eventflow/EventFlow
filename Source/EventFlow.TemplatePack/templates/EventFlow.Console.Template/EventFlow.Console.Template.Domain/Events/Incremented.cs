using EventFlow.Aggregates;
using EventFlow.EventStores;

namespace EventFlow.Console.Template.Domain.Events
{
    [EventVersion(nameof(Incremented), 1)]
    public class Incremented : AggregateEvent<Entity, EntityId> { }
}
