using EventFlow.Aggregates;

namespace EventFlow.Console.Template.Domain.Events
{
    public class Decremented : AggregateEvent<Entity, EntityId> { }
}
