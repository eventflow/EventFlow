// The MIT License (MIT)
// 
// Copyright (c) 2015-2016 Rasmus Mikkelsen
// Copyright (c) 2015-2016 eBay Software Foundation
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
// 
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.Commands;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.ValueObjects;
using EventFlow.Examples.Shipping.ExternalServices.Routing;
using EventFlow.Exceptions;

namespace EventFlow.Examples.Shipping.Application
{
    public class BookingApplicationService : IBookingApplicationService
    {
        private readonly ICommandBus _commandBus;
        private readonly IRoutingService _routingService;

        public BookingApplicationService(
            ICommandBus commandBus,
            IRoutingService routingService)
        {
            _commandBus = commandBus;
            _routingService = routingService;
        }

        public async Task<CargoId> BookCargoAsync(Route route, CancellationToken cancellationToken)
        {
            var cargoId = CargoId.New;
            await _commandBus.PublishAsync(new CargoBookCommand(cargoId, route), cancellationToken).ConfigureAwait(false);

            var itineraries = await _routingService.CalculateItinerariesAsync(route, cancellationToken).ConfigureAwait(false);

            var itinerary = itineraries.FirstOrDefault();
            if (itinerary == null)
            {
                throw DomainError.With("Could not find itinerary");
            }

            await _commandBus.PublishAsync(new CargoSetItineraryCommand(cargoId, itinerary), cancellationToken).ConfigureAwait(false);

            return cargoId;
        }
    }
}