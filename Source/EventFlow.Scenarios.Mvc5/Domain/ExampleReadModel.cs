using EventFlow.Aggregates;
using EventFlow.ReadStores;

namespace EventFlow.Scenarios.Mvc5.Domain
{
    public class ExampleReadModel : IReadModel,
        IAmReadModelFor<ExampleAggrenate, ExampleId, ExampleEvent>
    {
        public int MagicNumber { get; private set; }

        public void Apply(
            IReadModelContext context,
            IDomainEvent<ExampleAggrenate, ExampleId, ExampleEvent> domainEvent)
        {
            MagicNumber = domainEvent.AggregateEvent.MagicNumber;
        }
    }
}