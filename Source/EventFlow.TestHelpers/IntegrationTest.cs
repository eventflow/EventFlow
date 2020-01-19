// The MIT License (MIT)
// 
// Copyright (c) 2015-2018 Rasmus Mikkelsen
// Copyright (c) 2015-2018 eBay Software Foundation
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.Queries;
using EventFlow.ReadStores;
using EventFlow.Sagas;
using EventFlow.Snapshots;
using EventFlow.Snapshots.Stores;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.Queries;
using EventFlow.TestHelpers.Aggregates.Sagas;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using EventFlow.TestHelpers.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EventFlow.TestHelpers
{
    public abstract class IntegrationTest: Test
    {
        protected IServiceProvider Resolver { get; private set; }
        protected IAggregateStore AggregateStore { get; private set; }
        protected IEventStore EventStore { get; private set; }
        protected ISnapshotStore SnapshotStore { get; private set; }
        protected ISnapshotPersistence SnapshotPersistence { get; private set; }
        protected ISnapshotDefinitionService SnapshotDefinitionService { get; private set; }
        protected IEventPersistence EventPersistence { get; private set; }
        protected IQueryProcessor QueryProcessor { get; private set; }
        protected ICommandBus CommandBus { get; private set; }
        protected ISagaStore SagaStore { get; private set; }
        protected IReadModelPopulator ReadModelPopulator { get; private set; }

        [SetUp]
        public void SetUpIntegrationTest()
        {
            var serviceCollection = new ServiceCollection()
                .AddScoped<IScopedContext, ScopedContext>();

            Options(serviceCollection.AddEventFlow());

                //.AddQueryHandler<DbContextQueryHandler, DbContextQuery, string>() TODO
                /*.AddDefaults(EventFlowTestHelpers.Assembly, 
                    type => type != typeof(DbContextQueryHandler))*/;

            Resolver = serviceCollection.BuildServiceProvider(true);

            AggregateStore = Resolver.GetRequiredService<IAggregateStore>();
            EventStore = Resolver.GetRequiredService<IEventStore>();
            SnapshotStore = Resolver.GetRequiredService<ISnapshotStore>();
            SnapshotPersistence = Resolver.GetRequiredService<ISnapshotPersistence>();
            SnapshotDefinitionService = Resolver.GetRequiredService<ISnapshotDefinitionService>();
            EventPersistence = Resolver.GetRequiredService<IEventPersistence>();
            CommandBus = Resolver.GetRequiredService<ICommandBus>();
            QueryProcessor = Resolver.GetRequiredService<IQueryProcessor>();
            ReadModelPopulator = Resolver.GetRequiredService<IReadModelPopulator>();
            SagaStore = Resolver.GetRequiredService<ISagaStore>();
        }

        [TearDown]
        public void TearDownIntegrationTest()
        {
            (Resolver as IDisposable)?.Dispose();
        }

        protected virtual IEventFlowBuilder Options(IEventFlowBuilder eventFlowSetup)
        {
            return eventFlowSetup;
        }

        protected Task<ThingyAggregate> LoadAggregateAsync(ThingyId thingyId)
        {
            return AggregateStore.LoadAsync<ThingyAggregate, ThingyId>(thingyId);
        }

        protected async Task<PingId> PublishPingCommandAsync(
            ThingyId thingyId,
            CancellationToken cancellationToken = default)
        {
            var pingIds = await PublishPingCommandsAsync(thingyId, 1, cancellationToken).ConfigureAwait(false);
            return pingIds.Single();
        }

        protected Task<ThingySaga> LoadSagaAsync(ThingyId thingyId)
        {
            // This is specified in the ThingySagaLocator
            var expectedThingySagaId = new ThingySagaId($"saga-{thingyId.Value}");

            return AggregateStore.LoadAsync<ThingySaga, ThingySagaId>(
                expectedThingySagaId,
                CancellationToken.None);
        }

        protected async Task<IReadOnlyCollection<PingId>> PublishPingCommandsAsync(
            ThingyId thingyId,
            int count, 
            CancellationToken cancellationToken = default)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var pingIds = new List<PingId>();

            for (var i = 0; i < count; i++)
            {
                var pingId = PingId.New;
                await CommandBus.PublishAsync(new ThingyPingCommand(thingyId, pingId), cancellationToken).ConfigureAwait(false);
                pingIds.Add(pingId);
            }

            return pingIds;
        }
    }
}
