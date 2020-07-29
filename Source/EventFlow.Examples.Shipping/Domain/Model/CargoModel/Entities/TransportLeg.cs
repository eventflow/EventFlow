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
using EventFlow.Entities;
using EventFlow.Examples.Shipping.Domain.Model.LocationModel;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel.Entities;

namespace EventFlow.Examples.Shipping.Domain.Model.CargoModel.Entities
{
    public class TransportLeg : Entity<TransportLegId>
    {
        public TransportLeg(
            TransportLegId id,
            LocationId loadLocation,
            LocationId unloadLocation,
            DateTimeOffset loadTime,
            DateTimeOffset unloadTime,
            VoyageId voyageId,
            CarrierMovementId carrierMovementId)
            : base(id)
        {
            if (loadLocation == null) throw new ArgumentNullException(nameof(loadLocation));
            if (unloadLocation == null) throw new ArgumentNullException(nameof(unloadLocation));
            if (loadTime == default(DateTimeOffset)) throw new ArgumentOutOfRangeException(nameof(loadTime));
            if (unloadTime == default(DateTimeOffset)) throw new ArgumentOutOfRangeException(nameof(unloadTime));
            if (voyageId == null) throw new ArgumentNullException(nameof(voyageId));
            if (carrierMovementId == null) throw new ArgumentNullException(nameof(carrierMovementId));

            LoadLocation = loadLocation;
            UnloadLocation = unloadLocation;
            LoadTime = loadTime;
            UnloadTime = unloadTime;
            VoyageId = voyageId;
            CarrierMovementId = carrierMovementId;
        }

        public LocationId LoadLocation { get; }
        public LocationId UnloadLocation { get; }
        public DateTimeOffset LoadTime { get; }
        public DateTimeOffset UnloadTime { get; }
        public VoyageId VoyageId { get; }
        public CarrierMovementId CarrierMovementId { get; } // TODO: Do we really want this?
    }
}