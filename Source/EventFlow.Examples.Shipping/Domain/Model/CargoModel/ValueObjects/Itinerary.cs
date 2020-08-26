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

using System;
using System.Collections.Generic;
using System.Linq;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.Entities;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.Specifications;
using EventFlow.Examples.Shipping.Domain.Model.LocationModel;
using EventFlow.Extensions;
using EventFlow.ValueObjects;

namespace EventFlow.Examples.Shipping.Domain.Model.CargoModel.ValueObjects
{
    public class Itinerary : ValueObject
    {
        public Itinerary(
            IEnumerable<TransportLeg> transportLegs)
        {
            var legsList = (transportLegs ?? Enumerable.Empty<TransportLeg>()).ToList();

            if (!legsList.Any()) throw new ArgumentException(nameof(transportLegs));
            (new TransportLegsAreConnectedSpecification()).ThrowDomainErrorIfNotSatisfied(legsList);

            TransportLegs = legsList;
        }

        public IReadOnlyList<TransportLeg> TransportLegs { get; }

        public LocationId DepartureLocation()
        {
            return TransportLegs.First().LoadLocation;
        }

        public DateTimeOffset DepartureTime()
        {
            return TransportLegs.First().UnloadTime;
        }

        public DateTimeOffset ArrivalTime()
        {
            return TransportLegs.Last().UnloadTime;
        }

        public LocationId ArrivalLocation()
        {
            return TransportLegs.Last().UnloadLocation;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            return TransportLegs;
        }
    }
}