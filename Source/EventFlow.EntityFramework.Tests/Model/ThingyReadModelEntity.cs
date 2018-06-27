using System.ComponentModel.DataAnnotations;
using EventFlow.Aggregates;
using EventFlow.ReadStores;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;

namespace EventFlow.EntityFramework.Tests.Model
{
    public class ThingyReadModelEntity : IReadModel,
        IAmReadModelFor<ThingyAggregate, ThingyId, ThingyDomainErrorAfterFirstEvent>,
        IAmReadModelFor<ThingyAggregate, ThingyId, ThingyPingEvent>,
        IAmReadModelFor<ThingyAggregate, ThingyId, ThingyDeletedEvent>
    {
        [Key]
        public string AggregateId { get; set; }

        public bool DomainErrorAfterFirstReceived { get; set; }

        public int PingsReceived { get; set; }

        public int Version { get; set; }

        public void Apply(IReadModelContext context,
            IDomainEvent<ThingyAggregate, ThingyId, ThingyDeletedEvent> domainEvent)
        {
            context.MarkForDeletion();
        }

        public void Apply(IReadModelContext context,
            IDomainEvent<ThingyAggregate, ThingyId, ThingyDomainErrorAfterFirstEvent> domainEvent)
        {
            DomainErrorAfterFirstReceived = true;
        }

        public void Apply(IReadModelContext context,
            IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent> domainEvent)
        {
            PingsReceived++;
        }

        public Thingy ToThingy()
        {
            return new Thingy(
                ThingyId.With(AggregateId),
                PingsReceived,
                DomainErrorAfterFirstReceived);
        }
    }
}
