// The MIT License (MIT)
// 
// Copyright (c) 2015-2017 Rasmus Mikkelsen
// Copyright (c) 2015-2017 eBay Software Foundation
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
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Aggregates.Queries;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using EventFlow.TestHelpers.Extensions;
using FluentAssertions;
using NUnit.Framework;
using Ploeh.AutoFixture;

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
            returnedThingyMessages.ShouldAllBeEquivalentTo(thingyMessages);
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
            returnedThingyMessages.ShouldAllBeEquivalentTo(returnedThingyMessages);
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

        private async Task<IReadOnlyCollection<ThingyMessage>> CreateAndPublishThingyMessagesAsync(ThingyId thingyId, int count)
        {
            var thingyMessages = Fixture.CreateMany<ThingyMessage>(count).ToList();
            await Task.WhenAll(thingyMessages.Select(m => CommandBus.PublishAsync(new ThingyAddMessageCommand(thingyId, m)))).ConfigureAwait(false);
            return thingyMessages;
        }

        protected abstract Type ReadModelType { get; }
    }
}