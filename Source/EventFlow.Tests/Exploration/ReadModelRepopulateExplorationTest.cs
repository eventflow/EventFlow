// The MIT License (MIT)
// 
// Copyright (c) 2015-2025 Rasmus Mikkelsen
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
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.ReadStores;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EventFlow.Tests.Exploration
{
    // Related https://github.com/eventflow/EventFlow/issues/1083
    public class ReadModelRepopulateExplorationTest
    {
        private IServiceProvider _serviceProvider;

        [SetUp]
        public void SetUp()
        {
            _serviceProvider = EventFlowOptions.New()
                .AddEvents(new[] { typeof(EventV1), typeof(EventV2) })
                .AddEventUpgraders(typeof(BrokenUpgradeV1ToV2))
                .UseInMemoryReadStoreFor<UpgradeReadModel>()
                .ServiceCollection.BuildServiceProvider();
        }

        [TearDown]
        public void TearDown()
        {
            (_serviceProvider as IDisposable)?.Dispose();
        }

        [Test]
        public async Task ActuallyStops()
        {
            // Arrange
            var id = BrokenId.New;
            var aggregateStore = _serviceProvider.GetRequiredService<IAggregateStore>();
            var readModelPopulator = _serviceProvider.GetRequiredService<IReadModelPopulator>();
            await aggregateStore.UpdateAsync<BrokenAggregate, BrokenId>(
                id,
                SourceId.New,
                (a, c) =>
                {
                    a.EmitUpgradeEventV1();
                    return Task.CompletedTask;
                },
                CancellationToken.None);

            // Act and Assert
            using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            Assert.ThrowsAsync<Exception>(() => readModelPopulator.PopulateAsync(typeof(UpgradeReadModel), timeoutSource.Token));
        }

        public class UpgradeReadModel : IReadModel,
            IAmReadModelFor<BrokenAggregate, BrokenId, EventV1>,
            IAmReadModelFor<BrokenAggregate, BrokenId, EventV2>
        {
            public Task ApplyAsync(
                IReadModelContext context,
                IDomainEvent<BrokenAggregate, BrokenId, EventV1> domainEvent,
                CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task ApplyAsync(
                IReadModelContext context,
                IDomainEvent<BrokenAggregate, BrokenId, EventV2> domainEvent,
                CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        public class BrokenId : Identity<BrokenId>
        {
            public BrokenId(string value) : base(value) { }
        }

        public class BrokenAggregate : AggregateRoot<BrokenAggregate, BrokenId>,
            IEmit<EventV1>,
            IEmit<EventV2>
        {
            public BrokenAggregate(BrokenId id) : base(id) { }

            public bool V1Applied { get; private set; }
            public bool V2Applied { get; private set; }

            public void EmitUpgradeEventV1()
            {
                Emit(new EventV1());
            }

            public void Apply(EventV1 aggregateEvent)
            {
                V1Applied = true;
            }

            public void Apply(EventV2 aggregateEvent)
            {
                V2Applied = true;
            }
        }

        public class EventV1 : IAggregateEvent<BrokenAggregate, BrokenId>
        {
        }

        public class EventV2 : IAggregateEvent<BrokenAggregate, BrokenId>
        {
        }

        public class BrokenUpgradeV1ToV2 : EventUpgraderNonAsync<BrokenAggregate, BrokenId>
        {
            protected override IEnumerable<IDomainEvent<BrokenAggregate, BrokenId>> Upgrade(
                IDomainEvent<BrokenAggregate, BrokenId> domainEvent)
            {
                throw new Exception("Always broken!");
            }
        }
    }
}
