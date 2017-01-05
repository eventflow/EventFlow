using EventFlow.Aggregates;
using EventFlow.ReadStores;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Aggregates.Events;

namespace EventFlow.Firebase.Tests.IntegrationTests.ReadModels
{
    public class FirebaseThingyMessageReadModel : IReadModel,
        IAmReadModelFor<ThingyAggregate, ThingyId, ThingyMessageAddedEvent>
    {
        public string Id { get; set; }

        public string ThingyId { get; set; }

        public string Message { get; set; }

        public void Apply(IReadModelContext context, IDomainEvent<ThingyAggregate, ThingyId, ThingyMessageAddedEvent> domainEvent)
        {
            ThingyId = domainEvent.AggregateIdentity.Value;

            var thingyMessage = domainEvent.AggregateEvent.ThingyMessage;
            Id = thingyMessage.Id.Value;
            Message = thingyMessage.Message;
        }

        public ThingyMessage ToThingyMessage()
        {
            return new ThingyMessage(
                ThingyMessageId.With(Id),
                Message);
        }
    }
}