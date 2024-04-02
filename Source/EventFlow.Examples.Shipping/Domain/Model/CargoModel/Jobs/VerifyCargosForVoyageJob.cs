// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.Queries;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel;
using EventFlow.Jobs;
using EventFlow.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace EventFlow.Examples.Shipping.Domain.Model.CargoModel.Jobs
{
    [JobVersion("VerifyCargosForVoyage", 1)]
    public class VerifyCargosForVoyageJob : IJob
    {
        public VoyageId VoyageId { get; }

        public VerifyCargosForVoyageJob(
            VoyageId voyageId)
        {
            VoyageId = voyageId;
        }

        public async Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            // Consideration: Fetching all cargos that are affected by an updated
            // schedule could potentially fetch several thousands. Each of these
            // potential re-routes would then take a considerable amount of time
            // and will thus be required to be executed in parallel

            var queryProcessor = serviceProvider.GetRequiredService<IQueryProcessor>();
            var jobScheduler = serviceProvider.GetRequiredService<IJobScheduler>();

            var cargos = await queryProcessor.ProcessAsync(new GetCargosDependentOnVoyageQuery(VoyageId), cancellationToken).ConfigureAwait(false);
            var jobs = cargos.Select(c => new VerifyCargoItineraryJob(c.Id));

            await Task.WhenAll(jobs.Select(j => jobScheduler.ScheduleNowAsync(j, cancellationToken))).ConfigureAwait(false);
        }
    }
}
