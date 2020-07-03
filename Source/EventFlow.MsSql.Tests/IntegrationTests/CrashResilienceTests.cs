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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.MsSql.Extensions;
using EventFlow.MsSql.ReliablePublish;
using EventFlow.PublishRecovery;
using EventFlow.Subscribers;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.MsSql;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.MsSql.Tests.IntegrationTests
{
    [Category(Categories.Integration)]
    public sealed class CrashResilienceTests : IntegrationTest
    {
        private IMsSqlDatabase _testDatabase;
        private TestPublisher _publisher;
        private PublishVerificator _publishVerificator;
        private RecoveryHandlerForTest _recoveryHandler;

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            _testDatabase = MsSqlHelpz.CreateDatabase("eventflow");

            _publisher = null;

            var resolver = eventFlowOptions
                .ConfigureMsSql(MsSqlConfiguration.New.SetConnectionString(_testDatabase.ConnectionString.Value))
                .UseMssqlReliablePublishing()
                .RegisterServices(sr => sr.Register<IRecoveryHandlerProcessor, RecoveryHandlerForTest>(Lifetime.Singleton))
                .RegisterServices(sr => sr.Decorate<IDomainEventPublisher>(
                                      (r, dea) =>
                                          _publisher ?? (_publisher = new TestPublisher(dea))))
                .RegisterServices(sr => sr.Register<IRecoveryDetector, AlwaysRecoverDetector>())
                .CreateResolver();

            var databaseMigrator = resolver.Resolve<IMsSqlDatabaseMigrator>();
            EventFlowPublishLogMsSql.MigrateDatabase(databaseMigrator);
            databaseMigrator.MigrateDatabaseUsingEmbeddedScripts(GetType().Assembly);

            _publisher = (TestPublisher)resolver.Resolve<IDomainEventPublisher>();
            _publishVerificator = (PublishVerificator)resolver.Resolve<IPublishVerificator>();
            _recoveryHandler = (RecoveryHandlerForTest)resolver.Resolve<IRecoveryHandlerProcessor>();

            return resolver;
        }

        [TearDown]
        public void TearDown()
        {
            _testDatabase.DisposeSafe("Failed to delete database");
        }

        [Test]
        public async Task ShouldRecoverAfterFailure()
        {
            // Arrange
            var id = ThingyId.New;
            _publisher.SimulatePublishFailure = true;
            await PublishPingCommandsAsync(id, 1).ConfigureAwait(false);

            // Act
            _publisher.SimulatePublishFailure = false;
            await Verify().ConfigureAwait(false);

            // Assert
            _recoveryHandler.RecoveredEvents.Should()
                .BeEquivalentTo(_publisher.NotPublishedEvents);
        }

        [Test]
        public async Task ShouldRetryVerification()
        {
            // Arrange
            var id = ThingyId.New;
            _publisher.SimulatePublishFailure = true;
            await PublishPingCommandsAsync(id, 1).ConfigureAwait(false);

            // Act
            var result = await _publishVerificator.VerifyOnceAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert
            result.Should().Be(PublishVerificationResult.RecoveredNeedVerify);
            _publisher.PublishedEvents.Should().BeEmpty();
        }

        [Test]
        public async Task ShouldNotRecoverAfterFailureWithoutVerificator()
        {
            // Arrange
            var id = ThingyId.New;
            _publisher.SimulatePublishFailure = true;

            // Act
            await PublishPingCommandsAsync(id, 1).ConfigureAwait(false);

            // Assert
            _recoveryHandler.RecoveredEvents.Should().BeEmpty();
        }

        private async Task Verify()
        {
            PublishVerificationResult result;
            do
            {
                result = await _publishVerificator.VerifyOnceAsync(CancellationToken.None).ConfigureAwait(false);
            } while (result != PublishVerificationResult.CompletedNoMoreDataToVerify);
        }

        private class RecoveryHandlerForTest : IRecoveryHandlerProcessor
        {
            private readonly List<IDomainEvent> _recoveredEvents = new List<IDomainEvent>();
            private readonly IReliableMarkProcessor _markProcessor;

            public RecoveryHandlerForTest(IReliableMarkProcessor markProcessor)
            {
                _markProcessor = markProcessor;
            }

            public IReadOnlyList<IDomainEvent> RecoveredEvents => _recoveredEvents;

            public Task RecoverAfterUnexpectedShutdownAsync(IReadOnlyList<IDomainEvent> eventsForRecovery, CancellationToken cancellationToken)
            {
                _recoveredEvents.AddRange(eventsForRecovery);

                return _markProcessor.MarkEventsPublishedAsync(eventsForRecovery);
            }
        }

        private class TestPublisher : IDomainEventPublisher
        {
            private readonly IDomainEventPublisher _inner;
            private readonly List<IDomainEvent> _publishedEvents = new List<IDomainEvent>();
            private readonly List<IDomainEvent> _notPublishedEvents = new List<IDomainEvent>();

            public TestPublisher(IDomainEventPublisher inner)
            {
                _inner = inner;
            }

            public bool SimulatePublishFailure { get; set; }

            public IReadOnlyList<IDomainEvent> PublishedEvents => _publishedEvents;

            public IReadOnlyList<IDomainEvent> NotPublishedEvents => _notPublishedEvents;

            public async Task PublishAsync(IReadOnlyCollection<IDomainEvent> domainEvents,
                CancellationToken cancellationToken)
            {
                if (SimulatePublishFailure)
                {
                    _notPublishedEvents.AddRange(domainEvents);
                    return;
                }

                await _inner.PublishAsync(domainEvents, cancellationToken);

                _publishedEvents.AddRange(domainEvents);
            }

            [Obsolete("Use PublishAsync (without generics and aggregate identity)")]
            public Task PublishAsync<TAggregate, TIdentity>(TIdentity id,
                IReadOnlyCollection<IDomainEvent> domainEvents,
                CancellationToken cancellationToken) where TAggregate : IAggregateRoot<TIdentity>
                where TIdentity : IIdentity
            {
                return _inner.PublishAsync<TAggregate, TIdentity>(id, domainEvents, cancellationToken);
            }
        }

        private sealed class AlwaysRecoverDetector : IRecoveryDetector
        {
            public bool IsNeedRecovery(IDomainEvent domainEvent)
            {
                return true;
            }
        }
    }
}