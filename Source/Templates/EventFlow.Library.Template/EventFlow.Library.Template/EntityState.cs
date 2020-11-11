using EventFlow.Aggregates;

namespace EventFlow.Library.Template
{
    public class EntityState : AggregateState<Entity, EntityId, EntityState>,
        IApply<Events.Incremented>,
        IApply<Events.Decremented>
    {
        void IApply<Events.Incremented>.Apply(Events.Incremented @event)
        {
            Value += 1;
        }

        void IApply<Events.Decremented>.Apply(Events.Decremented @event)
        {
            Value -= 1;
        }

        public int Value { get; private set; }
    }
}
