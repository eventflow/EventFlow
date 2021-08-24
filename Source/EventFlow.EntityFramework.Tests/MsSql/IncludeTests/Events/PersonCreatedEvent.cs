using EventFlow.Aggregates;

namespace EventFlow.EntityFramework.Tests.MsSql.IncludeTests.Events
{
    public class PersonCreatedEvent : AggregateEvent<PersonAggregate, PersonId>
    {
        public string Name { get; set; }

        public PersonCreatedEvent(string name)
        {
            Name = name;
        }
    }
}