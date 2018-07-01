using EventFlow.Aggregates;
using EventFlow.TestHelpers.Aggregates.Entities;

namespace EventFlow.TestHelpers.Aggregates.Events
{
    public class ThingyMessageHistoryAddedEvent : AggregateEvent<ThingyAggregate, ThingyId>
    {
        public ThingyMessageHistoryAddedEvent(ThingyMessage[] thingyMessages)
        {
            ThingyMessages = thingyMessages;
        }

        public ThingyMessage[] ThingyMessages { get; }
    }
}
