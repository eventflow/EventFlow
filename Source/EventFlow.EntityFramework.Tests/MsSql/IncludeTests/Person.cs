using System.Collections.Generic;
using EventFlow.Entities;

namespace EventFlow.EntityFramework.Tests.MsSql.IncludeTests
{
    public class Person : Entity<PersonId>
    {
        public string Name { get; }
        public ICollection<Address> Addresses { get; }
        public int NumberOfAddresses { get; }

        public Person(PersonId id, string name, ICollection<Address> addresses, int numberOfAddresses) : base(id)
        {
            Name = name;
            Addresses = addresses;
            NumberOfAddresses = numberOfAddresses;
        }
    }
}