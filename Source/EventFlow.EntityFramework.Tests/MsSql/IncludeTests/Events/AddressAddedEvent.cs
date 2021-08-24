using EventFlow.Aggregates;

namespace EventFlow.EntityFramework.Tests.MsSql.IncludeTests.Events
{
    public class AddressAddedEvent : AggregateEvent<PersonAggregate, PersonId>
    {
        public Address Address { get; set; }

        public AddressAddedEvent(Address address)
        {
            Address = address;
        }
    }
}