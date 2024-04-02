// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
// https://github.com/eventflow/EventFlow
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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