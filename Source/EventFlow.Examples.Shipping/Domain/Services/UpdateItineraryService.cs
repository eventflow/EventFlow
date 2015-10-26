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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.Entities;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.ValueObjects;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel.Queries;
using EventFlow.Queries;

namespace EventFlow.Examples.Shipping.Domain.Services
{
    public class UpdateItineraryService : IUpdateItineraryService
    {
        private readonly IQueryProcessor _queryProcessor;

        public UpdateItineraryService(
            IQueryProcessor queryProcessor)
        {
            _queryProcessor = queryProcessor;
        }

        public async Task<Itinerary> UpdateItineraryAsync(Itinerary itinerary, CancellationToken cancellationToken)
        {
            var voyageIds = itinerary.TransportLegs
                .Select(l => l.VoyageId)
                .Distinct();

            var voyages = await _queryProcessor.ProcessAsync(new GetVoyagesQuery(voyageIds), cancellationToken).ConfigureAwait(false);

            var carrierMovements = voyages
                .SelectMany(v => v.Schedule.CarrierMovements.Select(cm => new { VoyageId = v.Id, CarrierMovement = cm }))
                .ToDictionary(a => a.CarrierMovement.Id, a => a);

            var transportLegs = itinerary.TransportLegs.Select(l =>
                {
                    var a = carrierMovements[l.CarrierMovementId];
                    var cm = a.CarrierMovement;
                    return new TransportLeg(
                        l.Id,
                        cm.DepartureLocationId,
                        cm.ArrivalLocationId,
                        cm.DepartureTime,
                        cm.ArrivalTime,
                        a.VoyageId,
                        l.CarrierMovementId);
                });

            return new Itinerary(transportLegs);
        }
    }
}