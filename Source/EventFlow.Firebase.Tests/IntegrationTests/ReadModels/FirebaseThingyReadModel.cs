using EventFlow.Aggregates;
using EventFlow.ReadStores;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;

namespace EventFlow.Firebase.Tests.IntegrationTests.ReadModels
{
    public class FirebaseThingyReadModel : IReadModel,
        IAmReadModelFor<ThingyAggregate, ThingyId, ThingyDomainErrorAfterFirstEvent>,
        IAmReadModelFor<ThingyAggregate, ThingyId, ThingyPingEvent>
    {
        public string Id { get; set; }

        public bool DomainErrorAfterFirstReceived { get; set; }

        public int PingsReceived { get; set; }

        public void Apply(IReadModelContext context, IDomainEvent<ThingyAggregate, ThingyId, ThingyDomainErrorAfterFirstEvent> domainEvent)
        {
            Id = domainEvent.AggregateIdentity.Value;
            DomainErrorAfterFirstReceived = true;
        }

        public void Apply(IReadModelContext context, IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent> domainEvent)
        {
            Id = domainEvent.AggregateIdentity.Value;
            PingsReceived++;
        }

        public Thingy ToThingy()
        {
            return new Thingy(
                ThingyId.With(Id),
                PingsReceived,
                DomainErrorAfterFirstReceived);
        }
    }
}