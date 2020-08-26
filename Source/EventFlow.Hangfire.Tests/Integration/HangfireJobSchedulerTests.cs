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
using System.Threading.Tasks;
using EventFlow.Extensions;
using EventFlow.Hangfire.Extensions;
using EventFlow.Hangfire.Integration;
using EventFlow.TestHelpers;
using Hangfire;
using NUnit.Framework;
using EventFlow.Configuration;
using EventFlow.Jobs;
using EventFlow.TestHelpers.MsSql;
using EventFlow.TestHelpers.Suites;
using FluentAssertions;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.SqlServer;

namespace EventFlow.Hangfire.Tests.Integration
{
    [Category(Categories.Integration)]
    public class HangfireJobSchedulerTests : TestSuiteForScheduler
    {
        private IMsSqlDatabase _msSqlDatabase;
        private BackgroundJobServer _backgroundJobServer;
        private EventFlowResolverActivator _eventFlowResolverActivator;
        private HangfireJobLog _log;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _msSqlDatabase = MsSqlHelpz.CreateDatabase("hangfire");

            var sqlServerStorageOptions = new SqlServerStorageOptions 
                {
                    QueuePollInterval = TimeSpan.FromSeconds(1),
                };

            _log = new HangfireJobLog();

            var jobFilterCollection = new JobFilterCollection {_log};

            var backgroundJobServerOptions = new BackgroundJobServerOptions
                {
                    SchedulePollingInterval = TimeSpan.FromSeconds(1),
                    FilterProvider = jobFilterCollection
                };

            GlobalConfiguration.Configuration
                .UseSqlServerStorage(_msSqlDatabase.ConnectionString.Value, sqlServerStorageOptions)
                .UseActivator(new DelegatingActivator(() => _eventFlowResolverActivator));

            _backgroundJobServer = new BackgroundJobServer(backgroundJobServerOptions);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _backgroundJobServer.DisposeSafe("Hangfire background job server");
            _msSqlDatabase.DisposeSafe("MSSQL database");
        }

        private class DelegatingActivator : JobActivator
        {
            private readonly Func<EventFlowResolverActivator> _eventFlowResolverActivatorFetcher;

            public DelegatingActivator(Func<EventFlowResolverActivator> eventFlowResolverActivatorFetcher)
            {
                _eventFlowResolverActivatorFetcher = eventFlowResolverActivatorFetcher;
            }

            public override object ActivateJob(Type jobType)
            {
                return _eventFlowResolverActivatorFetcher().ActivateJob(jobType);
            }
        }

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            var resolver = eventFlowOptions
                .UseHangfireJobScheduler()
                .CreateResolver(false);

            _eventFlowResolverActivator = new EventFlowResolverActivator(resolver);

            return resolver;
        }

        protected override async Task AssertJobIsSuccessfullAsync(IJobId jobId)
        {
            var context = await _log.GetAsync(jobId.Value);
            context.Should().NotBeNull();
            context.Exception.Should().BeNull();
            var displayName = context.BackgroundJob.Job.Args[0].ToString();
            displayName.Should().Be("PublishCommand v1");
        }
    }
}