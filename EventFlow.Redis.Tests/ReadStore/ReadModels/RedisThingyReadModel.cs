using EventFlow.Aggregates;
using EventFlow.ReadStores;
using EventFlow.Redis.ReadStore;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;

namespace EventFlow.Redis.Tests.ReadStore.ReadModels;

public class RedisThingyReadModel : RedisReadModel, IAmReadModelFor<ThingyAggregate, ThingyId, ThingyDomainErrorAfterFirstEvent>,
    IAmReadModelFor<ThingyAggregate, ThingyId, ThingyPingEvent>,
    IAmReadModelFor<ThingyAggregate, ThingyId, ThingyDeletedEvent>
{
    
    public bool DomainErrorAfterFirstReceived { get; set; }
    public int PingsReceived { get; set; }
    
    public Task ApplyAsync(IReadModelContext context,
        IDomainEvent<ThingyAggregate, ThingyId, ThingyDomainErrorAfterFirstEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        Id = domainEvent.AggregateIdentity.Value;
        DomainErrorAfterFirstReceived = true;
            
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context,
        IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        Id = domainEvent.AggregateIdentity.Value;
        PingsReceived++;
            
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context,
        IDomainEvent<ThingyAggregate, ThingyId, ThingyDeletedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        context.MarkForDeletion();
            
        return Task.CompletedTask;
    }

    public Thingy ToThingy()
    {
        return new Thingy(
            ThingyId.With(Id),
            PingsReceived,
            DomainErrorAfterFirstReceived);
    }
}