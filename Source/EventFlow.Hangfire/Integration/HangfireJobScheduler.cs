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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Jobs;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace EventFlow.Hangfire.Integration
{
    public class HangfireJobScheduler : IJobScheduler
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IJobDefinitionService _jobDefinitionService;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger<HangfireJobScheduler> _logger;
        private readonly string _queueName;

        public HangfireJobScheduler(
            ILogger<HangfireJobScheduler> logger,
            IJsonSerializer jsonSerializer,
            IBackgroundJobClient backgroundJobClient,
            IJobDefinitionService jobDefinitionService,
            IQueueNameProvider queueNameProvider)
        {
            _logger = logger;
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
                (jobDefinition, json) =>
                    _queueName == null
                        ? _backgroundJobClient.Enqueue(ExecuteMethodCallExpression(jobDefinition, json))
                        : _backgroundJobClient.Enqueue(_queueName, ExecuteMethodCallExpression(jobDefinition, json)));
        }

        public Task<IJobId> ScheduleAsync(IJob job, DateTimeOffset runAt, CancellationToken cancellationToken)
        {
            return ScheduleAsync(
                job,
                cancellationToken,
                (jobDefinition, json) =>
                    _queueName == null
                        ? _backgroundJobClient.Enqueue(ExecuteMethodCallExpression(jobDefinition, json))
                        : _backgroundJobClient.Schedule(_queueName, ExecuteMethodCallExpression(jobDefinition, json), runAt));
        }

        public Task<IJobId> ScheduleAsync(IJob job, TimeSpan delay, CancellationToken cancellationToken)
        {
            return ScheduleAsync(
                job,
                cancellationToken,
                (jobDefinition, json) =>
                    _queueName == null
                        ? _backgroundJobClient.Enqueue(ExecuteMethodCallExpression(jobDefinition, json))
                        : _backgroundJobClient.Schedule(_queueName, ExecuteMethodCallExpression(jobDefinition, json), delay));
        }

        private Task<IJobId> ScheduleAsync(
            IJob job,
            CancellationToken cancellationToken,
            Func<JobDefinition, string, string> schedule)
        {
            try
            {
                var jobDefinition = _jobDefinitionService.GetDefinition(job.GetType());
                var json = _jsonSerializer.Serialize(job);

                var id = schedule(jobDefinition, json);

                _logger.LogInformation("Scheduled job {JobId} with name {JobDefinitionName} in Hangfire", id, jobDefinition.Name);

                return Task.FromResult<IJobId>(new HangfireJobId(id));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static Expression<Func<IHangfireJobRunner, Task>> ExecuteMethodCallExpression(JobDefinition jobDefinition, string json)
        {
            return r => r.ExecuteAsync(jobDefinition.Name, jobDefinition.Version, json);
        }
    }
}