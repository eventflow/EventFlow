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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.Logs;

namespace EventFlow.Jobs
{
    public class InstantJobScheduler : IJobScheduler
    {
        private readonly IJobDefinitionService _jobDefinitionService;
        private readonly IJobRunner _jobRunner;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILog _log;

        public InstantJobScheduler(
            ILog log,
            IJsonSerializer jsonSerializer,
            IJobRunner jobRunner,
            IJobDefinitionService jobDefinitionService)
        {
            _log = log;
            _jsonSerializer = jsonSerializer;
            _jobRunner = jobRunner;
            _jobDefinitionService = jobDefinitionService;
        }

        public async Task<IJobId> ScheduleNowAsync(IJob job, CancellationToken cancellationToken)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));

            var jobDefinition = _jobDefinitionService.GetDefinition(job.GetType());

            try
            {
                var json = _jsonSerializer.Serialize(job);

                _log.Verbose(() => $"Executing job '{jobDefinition.Name}' v{jobDefinition.Version}: {json}");

                await _jobRunner.ExecuteAsync(jobDefinition.Name, jobDefinition.Version, json, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // We want the InstantJobScheduler to behave as an out-of-process scheduler, i.e., doesn't
                // throw exceptions directly related to the job execution
                _log.Error(e, $"Execution of job '{jobDefinition.Name}' v{jobDefinition.Version} failed!");
            }

            return JobId.New;
        }

        public Task<IJobId> ScheduleAsync(IJob job, DateTimeOffset runAt, CancellationToken cancellationToken)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));

            _log.Warning($"Instant scheduling configured, executing job '{job.GetType().PrettyPrint()}' NOW! Instead of at '{runAt}'");

            // Don't schedule, just execute...
            return ScheduleNowAsync(job, cancellationToken);
        }

        public Task<IJobId> ScheduleAsync(IJob job, TimeSpan delay, CancellationToken cancellationToken)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));

            _log.Warning($"Instant scheduling configured, executing job '{job.GetType().PrettyPrint()}' NOW! Instead of in '{delay}'");

            // Don't schedule, just execute...
            return ScheduleNowAsync(job, cancellationToken);
        }
    }
}