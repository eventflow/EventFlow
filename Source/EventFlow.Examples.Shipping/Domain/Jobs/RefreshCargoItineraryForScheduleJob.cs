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

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.Queries;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel.Entities;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel.ValueObjects;
using EventFlow.Jobs;
using EventFlow.Queries;

namespace EventFlow.Examples.Shipping.Domain.Jobs
{
    [JobVersion("RefreshCargoItineraryForVoyage", 1)]
    public class RefreshCargoItineraryForScheduleJob : IJob
    {
        public VoyageId VoyageId { get; }
        public Schedule Schedule { get; }

        public RefreshCargoItineraryForScheduleJob(
            VoyageId voyageId,
            Schedule schedule)
        {
            VoyageId = voyageId;
            Schedule = schedule;
        }

        public async Task ExecuteAsync(IResolver resolver, CancellationToken cancellationToken)
        {
            // Consideration: Fetching all cargos that are affected by an updated
            // schedule could potentially fetch several thousands. Each of these
            // potential re-routes would then take a considerable amount of time
            // and will thus be required to be executed in parallel

            var queryProcessor = resolver.Resolve<IQueryProcessor>();

            var cargos = await queryProcessor.ProcessAsync(new GetCargosDependentOnScheduleQuery(Schedule), cancellationToken).ConfigureAwait(false);

            // TODO: Do something with the cargo here... need to evaluate itinerary according to Route specification
        }
    }
}