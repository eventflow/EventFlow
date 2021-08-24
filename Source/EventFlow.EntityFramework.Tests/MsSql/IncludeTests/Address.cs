using EventFlow.Entities;

namespace EventFlow.EntityFramework.Tests.MsSql.IncludeTests
{
    public class Address : Entity<AddressId>
    {
        public string Street { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string Country { get; set; }

        public Address(AddressId id, string street, string postalCode, string city, string country) : base(id)
        {
            Street = street;
            PostalCode = postalCode;
            City = city;
            Country = country;
        }
    }
}