using EventFlow.Aggregates;
using EventFlow.Exceptions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using FluentAssertions;
using NUnit.Framework;
using Ploeh.AutoFixture;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.EventStores.EventStore.Tests.UnitTests
{
    public class EventStoreEventPersistenceTests : TestsFor<EventStoreEventPersistence>
    {
        private int _aggregateSequenceNumber = 0;
        private IEventStoreConnection _eventStoreConnection;

        [SetUp]
        public void SetUp()
        {
            _aggregateSequenceNumber = 0;
            Fixture.Inject(_eventStoreConnection);
        }

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var connectionSettings = ConnectionSettings.Create().EnableVerboseLogging().KeepReconnecting().SetDefaultUserCredentials(new UserCredentials("admin", "changeit")).Build();
            _eventStoreConnection = EventStoreConnection.Create(connectionSettings, new IPEndPoint(IPAddress.Loopback, 1113));
            _eventStoreConnection.ConnectAsync().Wait();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            _eventStoreConnection.Close();
        }

        [Test]
        public async Task MultipleCommitsAreOk()
        {
            // Arrange
            var id = ThingyId.New;
            var events1 = EmitEvents(new ThingyPingEvent(PingId.New));
            var events2 = EmitEvents(new ThingyPingEvent(PingId.New), new ThingyPingEvent(PingId.New));

            // Act
            await Sut.CommitEventsAsync(id, events1, CancellationToken.None);
            await Sut.CommitEventsAsync(id, events2, CancellationToken.None);

            // Assert
            var committedEvents = await Sut.LoadCommittedEventsAsync(id, CancellationToken.None);
            committedEvents.Count.Should().Be(3);
        }

        [Test]
        public async Task MultipleCommitsAreIdempotent()
        {
            // Arrange
            var id = ThingyId.New;
            var events1 = EmitEvents(new ThingyPingEvent(PingId.New));

            // Act
            await Sut.CommitEventsAsync(id, events1, CancellationToken.None).ConfigureAwait(false);
            await Sut.CommitEventsAsync(id, events1, CancellationToken.None).ConfigureAwait(false);

            // Assert
            var committedEvents = await Sut.LoadCommittedEventsAsync(id, CancellationToken.None);
            committedEvents.Count.Should().Be(1);
        }

        [Test]
        [ExpectedException(typeof(OptimisticConcurrencyException))]
        public async Task ConcurrencyCommitsFails()
        {
            // Arrange
            var id = ThingyId.New;
            var events1 = EmitEvents(new ThingyPingEvent(PingId.New));
            _aggregateSequenceNumber = 0; // Fire concurrency
            var events2 = EmitEvents(new ThingyPingEvent(PingId.New));

            // Act
            await Sut.CommitEventsAsync(id, events1, CancellationToken.None).ConfigureAwait(false);
            await Sut.CommitEventsAsync(id, events2, CancellationToken.None).ConfigureAwait(false);
        }
        
        private ReadOnlyCollection<SerializedEvent> EmitEvents(params IAggregateEvent[] events)
        {
            var list = new List<SerializedEvent>();

            foreach (var p in events)
            {
                _aggregateSequenceNumber++;

                list.Add(new SerializedEvent(
                    string.Empty,
                    string.Empty,
                    _aggregateSequenceNumber,
                    new Metadata(new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>(MetadataKeys.AggregateName, "ThingyAggragate"),
                        new KeyValuePair<string, string>(MetadataKeys.AggregateSequenceNumber, _aggregateSequenceNumber.ToString()),
                        new KeyValuePair<string, string>(MetadataKeys.EventName, p.GetType().Name),
                        new KeyValuePair<string, string>(MetadataKeys.EventVersion, "1"),
                        new KeyValuePair<string, string>("guid", Guid.NewGuid().ToString())
                    })));
            }

            return new ReadOnlyCollection<SerializedEvent>(list);
        }
    }
}
