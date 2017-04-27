using EventFlow.Aggregates;
using EventFlow.Exceptions;

namespace EventFlow.Scenarios.Mvc5.Domain
{
    public class ExampleAggrenate : AggregateRoot<ExampleAggrenate, ExampleId>,
        IEmit<ExampleEvent>
    {
        private int? _magicNumber;

        public ExampleAggrenate(ExampleId id) : base(id) { }

        // Method invoked by our command
        public void SetMagicNumer(int magicNumber)
        {
            if (_magicNumber.HasValue)
                throw DomainError.With("Magic number already set");

            Emit(new ExampleEvent(magicNumber));
        }

        // We apply the event as part of the event sourcing system. EventFlow
        // provides several different methods for doing this, e.g. state objects,
        // the Apply method is merely the simplest
        public void Apply(ExampleEvent aggregateEvent)
        {
            _magicNumber = aggregateEvent.MagicNumber;
        }
    }
}