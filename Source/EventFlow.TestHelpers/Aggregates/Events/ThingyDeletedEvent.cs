using EventFlow.Aggregates;
using EventFlow.EventStores;

namespace EventFlow.TestHelpers.Aggregates.Events
{
    [EventVersion("ThingyDeleted", 1)]
    public class ThingyDeletedEvent : AggregateEvent<ThingyAggregate, ThingyId>
    {
    }
}