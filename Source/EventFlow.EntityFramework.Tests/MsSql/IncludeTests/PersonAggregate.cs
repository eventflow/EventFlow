using EventFlow.Aggregates;
using EventFlow.EntityFramework.Tests.MsSql.IncludeTests.Events;

namespace EventFlow.EntityFramework.Tests.MsSql.IncludeTests
{
    [AggregateName("Person")]
    public class PersonAggregate : AggregateRoot<PersonAggregate, PersonId>,
        IEmit<PersonCreatedEvent>,
        IEmit<AddressAddedEvent>
    {
        public PersonAggregate(PersonId id) : base(id)
        {
        }

        public void Create(string name)
        {
            Emit(new PersonCreatedEvent(name));
        }

        public void AddAddress(Address address)
        {
            Emit(new AddressAddedEvent(address));
        }

        void IEmit<PersonCreatedEvent>.Apply(PersonCreatedEvent aggregateEvent)
        {
            // save name into field for later usage
            // ..
        }

        void IEmit<AddressAddedEvent>.Apply(AddressAddedEvent aggregateEvent)
        {
            // save address into field for later usage
            // ..
        }
    }
}