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
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel.Entities;
using EventFlow.ValueObjects;

namespace EventFlow.Examples.Shipping.Domain.Model.CargoModel.ValueObjects
{
    public class Leg : ValueObject
    {
        public Leg(
            LocationId loadLocation,
            LocationId unloadLocation,
            DateTimeOffset loadTime,
            DateTimeOffset unloadTime,
            CarrierMovementId carrierMovementId)
        {
            if (loadLocation == null) throw new ArgumentNullException(nameof(loadLocation));
            if (unloadLocation == null) throw new ArgumentNullException(nameof(unloadLocation));
            if (loadTime == default(DateTimeOffset)) throw new ArgumentOutOfRangeException(nameof(loadTime));
            if (unloadTime == default(DateTimeOffset)) throw new ArgumentOutOfRangeException(nameof(unloadTime));
            if (carrierMovementId == null) throw new ArgumentNullException(nameof(carrierMovementId));

            LoadLocation = loadLocation;
            UnloadLocation = unloadLocation;
            LoadTime = loadTime;
            UnloadTime = unloadTime;
            CarrierMovementId = carrierMovementId;
        }

        public LocationId LoadLocation { get; }
        public LocationId UnloadLocation { get; }
        public DateTimeOffset LoadTime { get; }
        public DateTimeOffset UnloadTime { get; }
        public CarrierMovementId CarrierMovementId { get; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return LoadLocation;
            yield return UnloadLocation;
            yield return LoadTime;
            yield return UnloadTime;
            yield return CarrierMovementId;
        }
    }
}