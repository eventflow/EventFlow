// The MIT License (MIT)
// 
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
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

using EventFlow.Core;
using EventFlow.Jobs;
using Hangfire;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<HangfireJobScheduler> _logger;
        private readonly IJobDisplayNameBuilder _jobDisplayNameBuilder;
        private readonly string _queueName;

        public HangfireJobScheduler(
            ILogger<HangfireJobScheduler> logger,
            IJobDisplayNameBuilder jobDisplayNameBuilder,
            IJsonSerializer jsonSerializer,
            IBackgroundJobClient backgroundJobClient,
            IJobDefinitionService jobDefinitionService,
            IQueueNameProvider queueNameProvider)
        {
            _logger = logger;
            _jobDisplayNameBuilder = jobDisplayNameBuilder;
            _jsonSerializer = jsonSerializer;
            _backgroundJobClient = backgroundJobClient;
            _jobDefinitionService = jobDefinitionService;
            _queueName = queueNameProvider.QueueName;
        }

        public Task<IJobId> ScheduleNowAsync(IJob job, CancellationToken cancellationToken)
        {
            return ScheduleAsync(
                job,
                cancellationToken,
                (c, d, n, j) => _backgroundJobClient.Enqueue<IHangfireJobRunner>(r => r.ExecuteAsync(n, d.Name, d.Version, j, _queueName)));
        }

        public Task<IJobId> ScheduleAsync(IJob job, DateTimeOffset runAt, CancellationToken cancellationToken)
        {
            return ScheduleAsync(
                job,
                cancellationToken,
                (c, d, n, j) => _backgroundJobClient.Schedule<IHangfireJobRunner>(r => r.ExecuteAsync(n, d.Name, d.Version, j, _queueName), runAt));
        }

        public Task<IJobId> ScheduleAsync(IJob job, TimeSpan delay, CancellationToken cancellationToken)
        {
            return ScheduleAsync(
                job,
                cancellationToken,
                (c, d, n, j) => _backgroundJobClient.Schedule<IHangfireJobRunner>(r => r.ExecuteAsync(n, d.Name, d.Version, j, _queueName), delay));
        }

        private async Task<IJobId> ScheduleAsync(
            IJob job,
            CancellationToken cancellationToken,
            Func<IBackgroundJobClient, JobDefinition, string, string, string> schedule)
        {
            var jobDefinition = _jobDefinitionService.GetDefinition(job.GetType());
            var json = _jsonSerializer.Serialize(job);
            var name = await _jobDisplayNameBuilder.GetDisplayNameAsync(job, jobDefinition, cancellationToken).ConfigureAwait(false);

            var id = schedule(_backgroundJobClient, jobDefinition, name, json);

            _logger.LogTrace($"Scheduled job '{id}' with name '{name}' in Hangfire");

            return new HangfireJobId(id);
        }
    }
}