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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Logs;
using EventFlow.ReadStores;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Aggregates.Queries;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using EventFlow.TestHelpers.Extensions;
using AutoFixture;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.TestHelpers.Suites
{
    public abstract class TestSuiteForReadModelStore : IntegrationTest
    {
        [Test]
        public async Task NonExistingReadModelReturnsNull()
        {
            // Arrange
            var id = ThingyId.New;

            // Act
            var readModel = await QueryProcessor.ProcessAsync(new ThingyGetQuery(id)).ConfigureAwait(false);

            // Assert
            readModel.Should().BeNull();
        }

        [Test]
        public async Task ReadModelReceivesEvent()
        {
            // Arrange
            var id = ThingyId.New;
            
            // Act
            await PublishPingCommandsAsync(id, 5).ConfigureAwait(false);
            var readModel = await QueryProcessor.ProcessAsync(new ThingyGetQuery(id)).ConfigureAwait(false);

            // Assert
            readModel.Should().NotBeNull();
            readModel.PingsReceived.Should().Be(5);
        }

        [Test]
        public async Task InitialReadModelVersionIsNull()
        {
            // Arrange
            var thingyId = ThingyId.New;

            // Act
            var version = await QueryProcessor.ProcessAsync(new ThingyGetVersionQuery(thingyId)).ConfigureAwait(false);

            // Assert
            version.Should().NotHaveValue();
        }

        [Test]
        public async Task ReadModelVersionShouldMatchAggregate()
        {
            // Arrange
            var thingyId = ThingyId.New;
            const int expectedVersion = 5;
            await PublishPingCommandsAsync(thingyId, expectedVersion).ConfigureAwait(false);

            // Act
            var version = await QueryProcessor.ProcessAsync(new ThingyGetVersionQuery(thingyId)).ConfigureAwait(false);

            // Assert
            version.Should().Be((long)version);
        }

        [Test]
        public async Task CanStoreMultipleMessages()
        {
            // Arrange
            var thingyId = ThingyId.New;
            var otherThingyId = ThingyId.New;
            var thingyMessages = await CreateAndPublishThingyMessagesAsync(thingyId, 5).ConfigureAwait(false);
            await CreateAndPublishThingyMessagesAsync(otherThingyId, 3).ConfigureAwait(false);

            // Act
            var returnedThingyMessages = await QueryProcessor.ProcessAsync(new ThingyGetMessagesQuery(thingyId)).ConfigureAwait(false);

            // Assert
            returnedThingyMessages.Should().HaveCount(thingyMessages.Count);
            returnedThingyMessages.Should().BeEquivalentTo(thingyMessages);
        }

        [Test]
        public async Task CanHandleMultipleMessageAtOnce()
        {
            // Arrange
            var thingyId = ThingyId.New;
            var pingIds = Many<PingId>(5);
            var thingyMessages = Many<ThingyMessage>(7);

            // Act
            await CommandBus.PublishAsync(new ThingyImportCommand(
                thingyId,
                pingIds,
                thingyMessages))
                .ConfigureAwait(false);
            var returnedThingyMessages = await QueryProcessor.ProcessAsync(new ThingyGetMessagesQuery(thingyId)).ConfigureAwait(false);
            var thingy = await QueryProcessor.ProcessAsync(new ThingyGetQuery(thingyId)).ConfigureAwait(false);

            // Assert
            thingy.PingsReceived.Should().Be(pingIds.Count);
            returnedThingyMessages.Should().BeEquivalentTo(thingyMessages);
        }

        [Test]
        public async Task PurgeRemovesReadModels()
        {
            // Arrange
            var id = ThingyId.New;
            await PublishPingCommandAsync(id).ConfigureAwait(false);

            // Act
            await ReadModelPopulator.PurgeAsync(ReadModelType, CancellationToken.None).ConfigureAwait(false);
            var readModel = await QueryProcessor.ProcessAsync(new ThingyGetQuery(id)).ConfigureAwait(false);

            // Assert
            readModel.Should().BeNull();
        }

        [Test]
        public async Task DeleteRemovesSpecificReadModel()
        {
            // Arrange
            var id1 = ThingyId.New;
            var id2 = ThingyId.New;
            await PublishPingCommandAsync(id1).ConfigureAwait(false);
            await PublishPingCommandAsync(id2).ConfigureAwait(false);
            var readModel1 = await QueryProcessor.ProcessAsync(new ThingyGetQuery(id1)).ConfigureAwait(false);
            var readModel2 = await QueryProcessor.ProcessAsync(new ThingyGetQuery(id2)).ConfigureAwait(false);
            readModel1.Should().NotBeNull();
            readModel2.Should().NotBeNull();

            // Act
            await ReadModelPopulator.DeleteAsync(
                id1.Value,
                ReadModelType,
                CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            readModel1 = await QueryProcessor.ProcessAsync(new ThingyGetQuery(id1)).ConfigureAwait(false);
            readModel2 = await QueryProcessor.ProcessAsync(new ThingyGetQuery(id2)).ConfigureAwait(false);
            readModel1.Should().BeNull();
            readModel2.Should().NotBeNull();
        }

        [Test]
        public async Task RePopulateHandlesManyAggregates()
        {
            // Arrange
            var id1 = ThingyId.New;
            var id2 = ThingyId.New;
            await PublishPingCommandsAsync(id1, 3).ConfigureAwait(false);
            await PublishPingCommandsAsync(id2, 5).ConfigureAwait(false);

            // Act
            await ReadModelPopulator.PurgeAsync(ReadModelType, CancellationToken.None).ConfigureAwait(false);
            await ReadModelPopulator.PopulateAsync(ReadModelType, CancellationToken.None).ConfigureAwait(false);

            // Assert
            var readModel1 = await QueryProcessor.ProcessAsync(new ThingyGetQuery(id1)).ConfigureAwait(false);
            var readModel2 = await QueryProcessor.ProcessAsync(new ThingyGetQuery(id2)).ConfigureAwait(false);

            readModel1.PingsReceived.Should().Be(3);
            readModel2.PingsReceived.Should().Be(5);
        }

        [Test]
        public async Task PopulateCreatesReadModels()
        {
            // Arrange
            var id = ThingyId.New;
            await PublishPingCommandsAsync(id, 2).ConfigureAwait(false);
            await ReadModelPopulator.PurgeAsync(ReadModelType, CancellationToken.None).ConfigureAwait(false);
            
            // Act
            await ReadModelPopulator.PopulateAsync(ReadModelType, CancellationToken.None).ConfigureAwait(false);
            var readModel = await QueryProcessor.ProcessAsync(new ThingyGetQuery(id)).ConfigureAwait(false);

            // Assert
            readModel.Should().NotBeNull();
            readModel.PingsReceived.Should().Be(2);
        }

        [Test]
        public async Task MultipleUpdatesAreHandledCorrect()
        {
            // Arrange
            var id = ThingyId.New;
            var pingIds = new List<PingId>
                {
                    await PublishPingCommandAsync(id).ConfigureAwait(false)
                };

            for (var i = 0; i < 5; i++)
            {
                // Act
                pingIds.Add(await PublishPingCommandAsync(id).ConfigureAwait(false));

                // Assert
                var readModel = await QueryProcessor.ProcessAsync(new ThingyGetQuery(id)).ConfigureAwait(false);
                readModel.PingsReceived.Should().Be(pingIds.Count);
            }
        }

        [Test]
        public virtual async Task OptimisticConcurrencyCheck()
        {
            // Simulates a state in which two read models have been loaded to memory
            // and each is updated independently. The read store should detect the
            // concurrent update, reload the read model and apply the updates once
            // again.
            // A decorated DelayingReadModelDomainEventApplier is used to introduce
            // a controlled delay and a set of AutoResetEvent is used to ensure
            // that the read store is in the desired state before continuing

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                // Arrange
                var id = ThingyId.New;
                var waitState = new WaitState();
                await PublishPingCommandsAsync(id, 1, cts.Token).ConfigureAwait(false);

                // Arrange
                _waitStates[id.Value] = waitState;
                var delayedPublishTask = Task.Run(() => PublishPingCommandsAsync(id, 1, cts.Token), cts.Token);
                waitState.ReadStoreReady.WaitOne(TimeSpan.FromSeconds(10));
                _waitStates.Remove(id.Value);
                await PublishPingCommandsAsync(id, 1, cts.Token).ConfigureAwait(false);
                waitState.ReadStoreContinue.Set();
                await delayedPublishTask.ConfigureAwait(false);

                // Assert
                var readModel = await QueryProcessor.ProcessAsync(new ThingyGetQuery(id), cts.Token).ConfigureAwait(false);
                readModel.PingsReceived.Should().Be(3);
            }
        }

        [Test]
        public async Task MarkingForDeletionRemovesSpecificReadModel()
        {
            // Arrange
            var id1 = ThingyId.New;
            var id2 = ThingyId.New;
            await PublishPingCommandAsync(id1).ConfigureAwait(false);
            await PublishPingCommandAsync(id2).ConfigureAwait(false);
            var readModel1 = await QueryProcessor.ProcessAsync(new ThingyGetQuery(id1)).ConfigureAwait(false);
            var readModel2 = await QueryProcessor.ProcessAsync(new ThingyGetQuery(id2)).ConfigureAwait(false);
            readModel1.Should().NotBeNull();
            readModel2.Should().NotBeNull();

            // Act
            await CommandBus.PublishAsync(new ThingyDeleteCommand(id1), CancellationToken.None);

            // Assert
            readModel1 = await QueryProcessor.ProcessAsync(new ThingyGetQuery(id1)).ConfigureAwait(false);
            readModel2 = await QueryProcessor.ProcessAsync(new ThingyGetQuery(id2)).ConfigureAwait(false);
            readModel1.Should().BeNull();
            readModel2.Should().NotBeNull();
        }

        [Test]
        public async Task CanStoreMessageHistory()
        {
            // Arrange
            var thingyId = ThingyId.New;
            var thingyMessages = Fixture.CreateMany<ThingyMessage>(5).ToList();
            var command = new ThingyAddMessageHistoryCommand(thingyId, thingyMessages);
            await CommandBus.PublishAsync(command, CancellationToken.None);

            // Act
            var returnedThingyMessages = await QueryProcessor.ProcessAsync(new ThingyGetMessagesQuery(thingyId)).ConfigureAwait(false);

            // Assert
            returnedThingyMessages.Should().HaveCount(thingyMessages.Count);
            returnedThingyMessages.Should().BeEquivalentTo(thingyMessages);
        }

        private class WaitState
        {
            public AutoResetEvent ReadStoreReady { get; } = new AutoResetEvent(false);
            public AutoResetEvent ReadStoreContinue { get; } = new AutoResetEvent(false);
        }

        private readonly Dictionary<string, WaitState> _waitStates = new Dictionary<string, WaitState>();

        protected override IEventFlowOptions Options(IEventFlowOptions eventFlowOptions)
        {
            _waitStates.Clear();

            return base.Options(eventFlowOptions)
                .RegisterServices(sr => sr.Decorate<IReadModelDomainEventApplier>(
                    (r, dea) => new DelayingReadModelDomainEventApplier(dea, _waitStates, r.Resolver.Resolve<ILog>())));
        }

        private async Task<IReadOnlyCollection<ThingyMessage>> CreateAndPublishThingyMessagesAsync(ThingyId thingyId, int count)
        {
            var thingyMessages = Fixture.CreateMany<ThingyMessage>(count).ToList();
            await Task.WhenAll(thingyMessages.Select(m => CommandBus.PublishAsync(new ThingyAddMessageCommand(thingyId, m)))).ConfigureAwait(false);
            return thingyMessages;
        }

        protected abstract Type ReadModelType { get; }

        private class DelayingReadModelDomainEventApplier : IReadModelDomainEventApplier
        {
            private readonly IReadModelDomainEventApplier _readModelDomainEventApplier;
            private readonly IReadOnlyDictionary<string, WaitState> _waitStates;
            private readonly ILog _log;

            public DelayingReadModelDomainEventApplier(
                IReadModelDomainEventApplier readModelDomainEventApplier,
                IReadOnlyDictionary<string, WaitState> waitStates,
                ILog log)
            {
                _readModelDomainEventApplier = readModelDomainEventApplier;
                _waitStates = waitStates;
                _log = log;
            }

            public async Task<bool> UpdateReadModelAsync<TReadModel>(
                TReadModel readModel,
                IReadOnlyCollection<IDomainEvent> domainEvents,
                IReadModelContext readModelContext,
                CancellationToken cancellationToken)
                where TReadModel : IReadModel
            {
                _waitStates.TryGetValue(domainEvents.First().GetIdentity().Value, out var waitState);

                if (waitState != null)
                {
                    _log.Information("Waiting for access to read model");
                    waitState.ReadStoreReady.Set();
                    waitState.ReadStoreContinue.WaitOne();
                }

                return await _readModelDomainEventApplier.UpdateReadModelAsync(
                    readModel,
                    domainEvents,
                    readModelContext,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}