// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
// https://github.com/rasmus/EventFlow
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
using System.Linq;
using EventFlow.Aggregates;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.Events;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.ValueObjects;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel.Entities;
using EventFlow.ReadStores;

namespace EventFlow.Examples.Shipping.Queries.InMemory.ReadModels
{
    public class CargoReadModel : IReadModel,
        IAmReadModelFor<CargoAggregate, CargoId, CargoItinerarySetEvent>,
        IAmReadModelFor<CargoAggregate, CargoId, CargoBookedEvent>
    {
        public CargoId Id { get; private set; }
        public HashSet<CarrierMovementId> DependentCarrierMovementIds { get; } = new HashSet<CarrierMovementId>();
        public Itinerary Itinerary { get; private set; }

        public void Apply(IReadModelContext context, IDomainEvent<CargoAggregate, CargoId, CargoBookedEvent> domainEvent)
        {
            Id = domainEvent.AggregateIdentity;
        }

        public void Apply(IReadModelContext context, IDomainEvent<CargoAggregate, CargoId, CargoItinerarySetEvent> domainEvent)
        {
            var carrierMovementIds = domainEvent.AggregateEvent.Itinerary.TransportLegs
                .Select(l => l.CarrierMovementId)
                .ToList();
            carrierMovementIds.ForEach(id => DependentCarrierMovementIds.Add(id));
            Itinerary = domainEvent.AggregateEvent.Itinerary;
        }

        public Cargo ToCargo()
        {
            return new Cargo(
                Id,
                Itinerary);
        }
    }
}