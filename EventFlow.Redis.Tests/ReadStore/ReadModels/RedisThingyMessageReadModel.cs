using EventFlow.Aggregates;
using EventFlow.ReadStores;
using EventFlow.Redis.ReadStore;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Aggregates.Events;
using Redis.OM.Modeling;

namespace EventFlow.Redis.Tests.ReadStore.ReadModels;

public class RedisThingyMessageReadModel : RedisReadModel,
    IAmReadModelFor<ThingyAggregate, ThingyId, ThingyMessageAddedEvent>,
    IAmReadModelFor<ThingyAggregate, ThingyId, ThingyMessageHistoryAddedEvent>
{
    [Indexed]public string ThingyId { get; set; }

    public string Message { get; set; }
    
    public Task ApplyAsync(IReadModelContext context, IDomainEvent<ThingyAggregate, ThingyId, ThingyMessageAddedEvent> domainEvent, CancellationToken cancellationToken)
    {
        var thingyMessage = domainEvent.AggregateEvent.ThingyMessage;

        Id = thingyMessage.Id.Value;
        ThingyId = domainEvent.AggregateIdentity.Value;
        Message = thingyMessage.Message;
            
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<ThingyAggregate, ThingyId, ThingyMessageHistoryAddedEvent> domainEvent, CancellationToken cancellationToken)
    {
        ThingyId = domainEvent.AggregateIdentity.Value;

        var messageId = new ThingyMessageId(context.ReadModelId);
        var thingyMessage = domainEvent.AggregateEvent.ThingyMessages.Single(m => m.Id == messageId);
        Id = messageId.Value;
        Message = thingyMessage.Message;
            
        return Task.CompletedTask;
    }
    
    public ThingyMessage ToThingyMessage()
    {
        return new ThingyMessage(
            ThingyMessageId.With(Id),
            Message);
    }

}