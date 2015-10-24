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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.Commands;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.Queries;
using EventFlow.Examples.Shipping.Domain.Services;
using EventFlow.Examples.Shipping.ExternalServices.Routing;
using EventFlow.Exceptions;
using EventFlow.Jobs;
using EventFlow.Queries;

namespace EventFlow.Examples.Shipping.Domain.Model.CargoModel.Jobs
{
    public class VerifyCargoItineraryJob : IJob
    {
        public VerifyCargoItineraryJob(
            CargoId cargoId)
        {
            CargoId = cargoId;
        }

        public CargoId CargoId { get; }

        public async Task ExecuteAsync(IResolver resolver, CancellationToken cancellationToken)
        {
            var queryProcessor = resolver.Resolve<IQueryProcessor>();
            var updateItineraryService = resolver.Resolve<IUpdateItineraryService>();
            var commandBus = resolver.Resolve<ICommandBus>();
            var routingService = resolver.Resolve<IRoutingService>();

            var cargo = (await queryProcessor.ProcessAsync(new GetCargosQuery(CargoId), cancellationToken).ConfigureAwait(false)).Single();
            var updatedItinerary = await updateItineraryService.UpdateItineraryAsync(cargo.Itinerary, cancellationToken).ConfigureAwait(false);

            if (cargo.Route.Specification().IsSatisfiedBy(updatedItinerary))
            {
                await commandBus.PublishAsync(new CargoSetItineraryCommand(cargo.Id, updatedItinerary), cancellationToken).ConfigureAwait(false);
                return;
            }

            var newItineraries = await routingService.CalculateItinerariesAsync(cargo.Route, cancellationToken).ConfigureAwait(false);

            var newItinerary = newItineraries.FirstOrDefault();
            if (newItinerary == null)
            {
                // TODO: Tell domain that a new itinerary could not be found
                throw DomainError.With("Could not find itinerary");
            }

            await commandBus.PublishAsync(new CargoSetItineraryCommand(cargo.Id, newItinerary), cancellationToken).ConfigureAwait(false);
        }
    }
}