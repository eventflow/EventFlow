using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using EventFlow.Aggregates;
using EventFlow.EntityFramework.Tests.MsSql.IncludeTests.Events;
using EventFlow.ReadStores;

namespace EventFlow.EntityFramework.Tests.MsSql.IncludeTests.ReadModels
{
    public class PersonReadModelEntity : IReadModel,
        IAmReadModelFor<PersonAggregate, PersonId, PersonCreatedEvent>,
        IAmReadModelFor<PersonAggregate, PersonId, AddressAddedEvent>
    {
        [Key] 
        [StringLength(64)]
        public string AggregateId { get; set; }

        public string Name { get; set; }

        public int NumberOfAddresses { get; set; }

        public virtual ICollection<AddressReadModelEntity> Addresses { get; set; } = new List<AddressReadModelEntity>();

        public void Apply(IReadModelContext context,
            IDomainEvent<PersonAggregate, PersonId, PersonCreatedEvent> domainEvent)
        {
            Name = domainEvent.AggregateEvent.Name;
        }

        public void Apply(IReadModelContext context,
            IDomainEvent<PersonAggregate, PersonId, AddressAddedEvent> domainEvent)
        {
            var address = domainEvent.AggregateEvent.Address;
            Addresses.Add(new AddressReadModelEntity
            {
                AddressId = address.Id.Value,
                PersonId = domainEvent.AggregateIdentity.Value,
                Street = address.Street,
                City = address.City,
                PostalCode = address.PostalCode,
                Country = address.Country
            });

            NumberOfAddresses = Addresses.Count;
        }

        public Person ToPerson() =>
            new Person(
                PersonId.With(AggregateId),
                Name, 
                Addresses?.Select(x => x.ToAddress()).ToList(),
                NumberOfAddresses);
    }
}