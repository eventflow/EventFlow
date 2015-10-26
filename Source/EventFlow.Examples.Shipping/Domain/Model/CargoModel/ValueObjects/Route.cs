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
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.Specifications;
using EventFlow.Examples.Shipping.Domain.Model.LocationModel;
using EventFlow.Examples.Shipping.Extensions;
using EventFlow.ValueObjects;

namespace EventFlow.Examples.Shipping.Domain.Model.CargoModel.ValueObjects
{
    public class Route : ValueObject
    {
        public Route(
            LocationId originLocationId,
            LocationId destinationLocationId,
            DateTimeOffset departureTime,
            DateTimeOffset arrivalDeadline)
        {
            if (originLocationId == null) throw new ArgumentNullException(nameof(originLocationId));
            if (destinationLocationId == null) throw new ArgumentNullException(nameof(destinationLocationId));
            if (arrivalDeadline == default(DateTimeOffset)) throw new ArgumentOutOfRangeException(nameof(arrivalDeadline));
            if (departureTime == default(DateTimeOffset)) throw new ArgumentOutOfRangeException(nameof(departureTime));
            if (originLocationId == destinationLocationId) throw new ArgumentException("Origin and destination cannot be the same");
            if (departureTime.IsAfter(arrivalDeadline)) throw new ArgumentException("Departure must be before arrival");

            OriginLocationId = originLocationId;
            DestinationLocationId = destinationLocationId;
            DepartureTime = departureTime;
            ArrivalDeadline = arrivalDeadline;
        }

        public LocationId OriginLocationId { get; }
        public LocationId DestinationLocationId { get; }
        public DateTimeOffset DepartureTime { get; }
        public DateTimeOffset ArrivalDeadline { get; }

        public RouteSpecification Specification()
        {
            return new RouteSpecification(this);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return OriginLocationId;
            yield return DestinationLocationId;
            yield return DepartureTime;
            yield return ArrivalDeadline;
        }
    }
}