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

using EventFlow.Core;
using EventFlow.Jobs;
using EventFlow.Logs;
using Hangfire;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.Hangfire.Integration
{
    public class HangfireJobScheduler : IJobScheduler
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IJobDefinitionService _jobDefinitionService;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILog _log;

        public HangfireJobScheduler(
            ILog log,
            IJsonSerializer jsonSerializer,
            IBackgroundJobClient backgroundJobClient,
            IJobDefinitionService jobDefinitionService)
        {
            _log = log;
            _jsonSerializer = jsonSerializer;
            _backgroundJobClient = backgroundJobClient;
            _jobDefinitionService = jobDefinitionService;
        }

        public Task<IJobId> ScheduleNowAsync(IJob job, CancellationToken cancellationToken)
        {
            return ScheduleAsync(job, (c, d, j) => _backgroundJobClient.Enqueue<IJobRunner>(r => r.Execute(d.Name, d.Version, j)));
        }

        public Task<IJobId> ScheduleAsync(IJob job, DateTimeOffset runAt, CancellationToken cancellationToken)
        {
            return ScheduleAsync(job, (c, d, j) => _backgroundJobClient.Schedule<IJobRunner>(r => r.Execute(d.Name, d.Version, j), runAt));
        }

        public Task<IJobId> ScheduleAsync(IJob job, TimeSpan delay, CancellationToken cancellationToken)
        {
            return ScheduleAsync(job, (c, d, j) => _backgroundJobClient.Schedule<IJobRunner>(r => r.Execute(d.Name, d.Version, j), delay));
        }

        private Task<IJobId> ScheduleAsync(IJob job, Func<IBackgroundJobClient, JobDefinition, string, string> schedule)
        {
            var jobDefinition = _jobDefinitionService.GetJobDefinition(job.GetType());
            var json = _jsonSerializer.Serialize(job);

            var id = schedule(_backgroundJobClient, jobDefinition, json);

            _log.Verbose($"Scheduled job '{id}' in Hangfire");

            return Task.FromResult<IJobId>(new HangfireJobId(id));
        }
    }
}