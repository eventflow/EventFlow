using System.ComponentModel.DataAnnotations;
using EventFlow.Aggregates;
using EventFlow.ReadStores;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Aggregates.Events;

namespace EventFlow.EntityFramework.Tests.Model
{
    public class ThingyMessageReadModelEntity: IReadModel,
        IAmReadModelFor<ThingyAggregate, ThingyId, ThingyMessageAddedEvent>
    {
        [Key]
        public string MessageId { get; set; }

        public string ThingyId { get; set; }

        public string Message { get; set; }

        public void Apply(IReadModelContext context, IDomainEvent<ThingyAggregate, ThingyId, ThingyMessageAddedEvent> domainEvent)
        {
            ThingyId = domainEvent.AggregateIdentity.Value;
            Message = domainEvent.AggregateEvent.ThingyMessage.Message;
        }

        public ThingyMessage ToThingyMessage()
        {
            return new ThingyMessage(
                ThingyMessageId.With(MessageId),
                Message);
        }
    }
}