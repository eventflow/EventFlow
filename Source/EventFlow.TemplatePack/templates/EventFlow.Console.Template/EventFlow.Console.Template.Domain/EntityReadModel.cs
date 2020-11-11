using EventFlow.Aggregates;
using EventFlow.Console.Template.Domain.Events;
using EventFlow.ReadStores;

namespace EventFlow.Console.Template.Domain
{
    public class EntityReadModel : IReadModel,
        IAmReadModelFor<Entity, EntityId, Events.Incremented>,
        IAmReadModelFor<Entity, EntityId, Events.Decremented>
    {
        public void Apply(IReadModelContext context, IDomainEvent<Entity, EntityId, Incremented> domainEvent)
        {
            Value += 1;
        }

        public void Apply(IReadModelContext context, IDomainEvent<Entity, EntityId, Decremented> domainEvent)
        {
            Value -= 1;
        }

        public int Value { get; private set; }
    }
}
