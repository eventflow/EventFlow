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
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Jobs;
using EventFlow.Provided.Jobs;
using EventFlow.Subscribers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using FluentAssertions;
using FluentAssertions.Common;
using NUnit.Framework;

namespace EventFlow.TestHelpers.Suites
{
    public abstract class TestSuiteForScheduler : IntegrationTest
    {
        private IJobScheduler _jobScheduler;
        private TestAsynchronousSubscriber _testAsynchronousSubscriber;

        private class TestAsynchronousSubscriber : ISubscribeAsynchronousTo<ThingyAggregate, ThingyId, ThingyPingEvent>
        {
            public BlockingCollection<PingId> PingIds { get; } = new BlockingCollection<PingId>();

            public Task HandleAsync(IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent> domainEvent, CancellationToken cancellationToken)
            {
                PingIds.Add(domainEvent.AggregateEvent.PingId, CancellationToken.None);
                return Task.FromResult(0);
            }
        }

        [SetUp]
        public void TestSuiteForSchedulerSetUp()
        {
            _jobScheduler = Resolver.Resolve<IJobScheduler>();
        }

        protected override IEventFlowOptions Options(IEventFlowOptions eventFlowOptions)
        {
            _testAsynchronousSubscriber = new TestAsynchronousSubscriber();

            return base.Options(eventFlowOptions)
                .RegisterServices(sr => sr.Register(c => (ISubscribeAsynchronousTo<ThingyAggregate, ThingyId, ThingyPingEvent>)_testAsynchronousSubscriber))
                .Configure(c => c.IsAsynchronousSubscribersEnabled = true);
        }

        [Test]
        public async Task AsynchronousSubscribesGetInvoked()
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                // Act
                var pingId = await PublishPingCommandAsync(A<ThingyId>(), cts.Token).ConfigureAwait(false);

                // Assert
                var receivedPingId = await Task.Run(() => _testAsynchronousSubscriber.PingIds.Take(), cts.Token).ConfigureAwait(false);
                receivedPingId.Should().IsSameOrEqualTo(pingId);
            }
        }

        [Test]
        public async Task ScheduleNow()
        {
            await ValidateScheduleHappens((j, s) => s.ScheduleNowAsync(j, CancellationToken.None)).ConfigureAwait(false);
        }

        [Test]
        public async Task ScheduleAsyncWithDateTime()
        {
            await ValidateScheduleHappens((j, s) => s.ScheduleAsync(j, DateTimeOffset.Now.AddSeconds(1), CancellationToken.None)).ConfigureAwait(false);
        }

        [Test]
        public async Task ScheduleAsyncWithTimeSpan()
        {
            await ValidateScheduleHappens((j, s) => s.ScheduleAsync(j, TimeSpan.FromSeconds(1), CancellationToken.None)).ConfigureAwait(false);
        }

        private async Task ValidateScheduleHappens(Func<IJob, IJobScheduler, Task<IJobId>> schedule)
        {
            // Arrange
            var testId = ThingyId.New;
            var pingId = PingId.New;
            var executeCommandJob = PublishCommandJob.Create(new ThingyPingCommand(testId, pingId), Resolver);

            // Act
            var jobId = await schedule(executeCommandJob, _jobScheduler).ConfigureAwait(false);

            // Assert
            var start = DateTimeOffset.Now;
            while (DateTimeOffset.Now < start + TimeSpan.FromSeconds(20))
            {
                var testAggregate = await AggregateStore.LoadAsync<ThingyAggregate, ThingyId>(testId, CancellationToken.None).ConfigureAwait(false);
                if (!testAggregate.IsNew)
                {
                    await AssertJobIsSuccessfullAsync(jobId).ConfigureAwait(false);
                    Assert.Pass();
                }

                await Task.Delay(TimeSpan.FromSeconds(0.2)).ConfigureAwait(false);
            }

            Assert.Fail("Aggregate did not receive the command as expected");
        }

        protected virtual Task AssertJobIsSuccessfullAsync(IJobId jobId)
        {
            // Overload to do any additional asserts
            return Task.FromResult(0);
        }
    }
}