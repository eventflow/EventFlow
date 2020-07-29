// The MIT License (MIT)
// 
// Copyright (c) 2015-2020 Rasmus Mikkelsen
// Copyright (c) 2015-2020 eBay Software Foundation
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
using EventFlow.Aggregates;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.Events;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.ValueObjects;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel;
using EventFlow.ReadStores;

namespace EventFlow.Examples.Shipping.Queries.InMemory.Cargos
{
    public class CargoReadModel : IReadModel,
        IAmReadModelFor<CargoAggregate, CargoId, CargoItinerarySetEvent>,
        IAmReadModelFor<CargoAggregate, CargoId, CargoBookedEvent>
    {
        public CargoId Id { get; private set; }
        public HashSet<VoyageId> DependentVoyageIds { get; } = new HashSet<VoyageId>();
        public Itinerary Itinerary { get; private set; }
        public Route Route { get; private set; }

        public void Apply(IReadModelContext context, IDomainEvent<CargoAggregate, CargoId, CargoBookedEvent> domainEvent)
        {
            Id = domainEvent.AggregateIdentity;
            Route = domainEvent.AggregateEvent.Route;
        }

        public void Apply(IReadModelContext context, IDomainEvent<CargoAggregate, CargoId, CargoItinerarySetEvent> domainEvent)
        {
            Itinerary = domainEvent.AggregateEvent.Itinerary;
            foreach (var transportLeg in domainEvent.AggregateEvent.Itinerary.TransportLegs)
            {
                DependentVoyageIds.Add(transportLeg.VoyageId);
            }
        }

        public Cargo ToCargo()
        {
            return new Cargo(
                Id,
                Route,
                Itinerary);
        }
    }
}