using EventFlow.Aggregates;

namespace EventFlow.Library.Template.Events
{
    public class Decremented : AggregateEvent<Entity, EntityId> { }
}
