using EventFlow.Aggregates;

namespace EventFlow.WebApp.Template.Domain.Events
{
    public class Decremented : AggregateEvent<Entity, EntityId> { }
}
