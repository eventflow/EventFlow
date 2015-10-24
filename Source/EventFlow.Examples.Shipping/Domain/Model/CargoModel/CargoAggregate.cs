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

using EventFlow.Aggregates;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.Events;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.ValueObjects;
using EventFlow.Extensions;

namespace EventFlow.Examples.Shipping.Domain.Model.CargoModel
{
    public class CargoAggregate : AggregateRoot<CargoAggregate, CargoId>
    {
        private readonly CargoState _state = new CargoState();

        public CargoAggregate(CargoId id) : base(id)
        {
            Register(_state);
        }

        public Route Route => _state.Route;
        public Itinerary Itinerary => _state.Itinerary;

        public void Book(Route route)
        {
            Specs.AggregateIsNew.ThrowDomainErrorIfNotStatisfied(this);
            Emit(new CargoBookedEvent(route));
        }

        public void SetItinerary(Itinerary itinerary)
        {
            Specs.AggregateIsCreated.ThrowDomainErrorIfNotStatisfied(this);
            Route.Specification().ThrowDomainErrorIfNotStatisfied(itinerary);

            Emit(new CargoItinerarySetEvent(itinerary));
        }
    }
}