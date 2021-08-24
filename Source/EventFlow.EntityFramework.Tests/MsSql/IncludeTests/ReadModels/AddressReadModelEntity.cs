using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventFlow.EntityFramework.Tests.MsSql.IncludeTests.ReadModels
{
    public class AddressReadModelEntity
    {
        [Key]
        [StringLength(64)]
        public string AddressId { get; set; }

        [StringLength(64)]
        public string PersonId { get; set; }

        [ForeignKey(nameof(PersonId))]
        public virtual PersonReadModelEntity Person { get; set; }

        public string Street { get; set; }

        public string PostalCode { get; set; }

        public string City { get; set; }

        public string Country { get; set; }

        public Address ToAddress() => new Address(IncludeTests.AddressId.With(AddressId), Street, PostalCode, City, Country);
    }
}