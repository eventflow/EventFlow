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

using System;
using System.Collections.Generic;
using EventFlow.Examples.Shipping.Domain.Model.LocationModel;
using EventFlow.Examples.Shipping.Extensions;
using EventFlow.ValueObjects;

namespace EventFlow.Examples.Shipping.Domain.Model.VoyageModel.ValueObjects
{
    public class CarrierMovement : ValueObject
    {
        public CarrierMovement(
            LocationId departureLocationId,
            LocationId arrivalLocationId,
            DateTimeOffset departureTime,
            DateTimeOffset arrivalTime)
        {
            if (departureLocationId == null) throw new ArgumentNullException(nameof(departureLocationId));
            if (arrivalLocationId == null) throw new ArgumentNullException(nameof(arrivalLocationId));
            if (departureTime == default(DateTimeOffset)) throw new ArgumentOutOfRangeException(nameof(departureTime));
            if (arrivalTime == default(DateTimeOffset)) throw new ArgumentOutOfRangeException(nameof(arrivalTime));
            if (departureTime.IsAfter(arrivalTime)) throw new ArgumentException("Arrival time must be after departure");

            DepartureLocationId = departureLocationId;
            ArrivalLocationId = arrivalLocationId;
            DepartureTime = departureTime;
            ArrivalTime = arrivalTime;
        }

        public LocationId DepartureLocationId { get; set; }
        public LocationId ArrivalLocationId { get; set; }
        public DateTimeOffset DepartureTime { get; set; }
        public DateTimeOffset ArrivalTime { get; set; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return DepartureLocationId;
            yield return ArrivalLocationId;
            yield return DepartureTime;
            yield return ArrivalTime;
        }
    }
}