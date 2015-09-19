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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.Hangfire.Extensions;
using EventFlow.Hangfire.Integration;
using EventFlow.Jobs;
using EventFlow.Provided.Jobs;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Test;
using EventFlow.TestHelpers.Aggregates.Test.Commands;
using EventFlow.TestHelpers.Aggregates.Test.ValueObjects;
using Hangfire;
using Helpz.MsSql;
using NUnit.Framework;

namespace EventFlow.Hangfire.Tests.Integration
{
    public class HangfireJobFlow : Test
    {
        private IMsSqlDatabase _msSqlDatabase;

        [SetUp]
        public void SetUp()
        {
            _msSqlDatabase = MsSqlHelpz.CreateDatabase("hangfire");
        }

        [TearDown]
        public void TearDown()
        {
            _msSqlDatabase.Dispose();
        }

        [Test]
        public async Task Flow()
        {
            using (var resolver = EventFlowOptions.New
                .AddDefaults(EventFlowTestHelpers.Assembly)
                .UseHandfireJobScheduler()
                .CreateResolver(false))
            {
                GlobalConfiguration.Configuration
                    .UseSqlServerStorage(_msSqlDatabase.ConnectionString.Value)
                    .UseActivator(new EventFlowResolverActivator(resolver));

                using (new BackgroundJobServer())
                {
                    // Arrange
                    var testId = TestId.New;
                    var pingId = PingId.New;
                    var jobScheduler = resolver.Resolve<IJobScheduler>();
                    var eventStore = resolver.Resolve<IEventStore>();
                    var executeCommandJob = PublishCommandJob.Create(new PingCommand(testId, pingId), resolver);

                    // Act
                    await jobScheduler.ScheduleNowAsync(executeCommandJob, CancellationToken.None).ConfigureAwait(false);

                    // Assert
                    var start = DateTimeOffset.Now;
                    while (DateTimeOffset.Now < start + TimeSpan.FromSeconds(20))
                    {
                        var testAggregate = await eventStore.LoadAggregateAsync<TestAggregate, TestId>(testId, CancellationToken.None).ConfigureAwait(false);
                        if (!testAggregate.IsNew)
                        {
                            Assert.Pass();
                        }
                        Thread.Sleep(TimeSpan.FromSeconds(0.2));
                    }
                    Assert.Fail();
                }
            }
        }
    }
}