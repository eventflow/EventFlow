using EventFlow.Aggregates;

namespace EventFlow.Scenarios.Mvc5.Domain
{
    public class ExampleEvent : AggregateEvent<ExampleAggrenate, ExampleId>
    {
        public ExampleEvent(int magicNumber)
        {
            MagicNumber = magicNumber;
        }

        public int MagicNumber { get; }
    }
}