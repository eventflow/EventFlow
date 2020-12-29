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
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.EventStores.InMemory;
using EventFlow.Logs;
using EventFlow.Snapshots;
using EventFlow.TestHelpers;
using FluentAssertions;
using Moq;
using NUnit.Framework;


namespace EventFlow.Tests.UnitTests.EventStores
{
	[Category(Categories.Unit)]
	public class EventStoreBaseTests
	{
		[Test]
		public async Task Different_events_from_different_aggregates_that_share_the_same_ID_should_not_conflict()
		{
			// Arrange
			const string metadataKey = "Test";

			var store = CreateStore();

			// This is the key point of the test.
			// Two distinctly separate aggregates define their own specific identity spaces.
			// However, in this particular test, the identities of the two aggregate happens
			// to be the same. If the EventStoreBase ignores the aggregate type, this 
			// would lead to events being mixed up between different aggregates because only
			// the aggregate identity is used to distinguish events from each other.
			const string aggregateId = "TestId";
			var firstId = new FirstAggregateId(aggregateId);
			var secondId = new SecondAggregateId(aggregateId);

			var eventsForFirstAggregate = new[]
				{
					new UncommittedEvent(
						new FirstTestEvent(),
						new Metadata(new KeyValuePair<string, string>(metadataKey, "Event for the first aggregate")
							)
								{
									AggregateSequenceNumber = 1,
									AggregateName = nameof(FirstAggregate),
									Timestamp = DateTimeOffset.Now
								})
				};

			var eventsForSecondAggregate = new[]
				{
					new UncommittedEvent(
						new SecondTestEvent(),
						new Metadata(new KeyValuePair<string, string>(metadataKey, "Event for the second aggregate")
							)
								{
									AggregateSequenceNumber = 2,
									AggregateName = nameof(SecondAggregate),
									Timestamp = DateTimeOffset.Now
								})
				};


			// Act
			await store.StoreAsync<FirstAggregate, FirstAggregateId>(firstId, eventsForFirstAggregate, SourceId.New, CancellationToken.None).ConfigureAwait(false);
			await store.StoreAsync<SecondAggregate, SecondAggregateId>(secondId, eventsForSecondAggregate, SourceId.New, CancellationToken.None).ConfigureAwait(false);


			// Assert
			var allEvents = await store.LoadAllEventsAsync(GlobalPosition.Start, int.MaxValue, CancellationToken.None);
			foreach (var @event in allEvents.DomainEvents)
				Console.WriteLine("EventType: {0}, AggregateType: {1}, Metadata: {2}", @event.EventType, @event.AggregateType, @event.Metadata[metadataKey]);
			allEvents.DomainEvents.Count.Should().Be(2, "because both events should be stored (but under the same AggregateId)");

			// Trying to load events for the FirstAggregate will throw an InvalidCastException because the SecondAggregate's events have been mixed up.
			// The reverse is true for the SecondAggregate.
			IReadOnlyCollection<IDomainEvent<FirstAggregate, FirstAggregateId>> eventsFromFirstAggregate = null;
			IReadOnlyCollection<IDomainEvent<SecondAggregate, SecondAggregateId>> eventsFromSecondAggregate = null;
			Assert.DoesNotThrowAsync(async () => eventsFromFirstAggregate = await store.LoadEventsAsync<FirstAggregate, FirstAggregateId>(firstId, CancellationToken.None));
			Assert.DoesNotThrowAsync(async () => eventsFromSecondAggregate = await store.LoadEventsAsync<SecondAggregate, SecondAggregateId>(secondId, CancellationToken.None));
			eventsFromFirstAggregate.Count.Should().Be(1);
			eventsFromSecondAggregate.Count.Should().Be(1);
		}


		private static EventStoreBase CreateStore()
		{
			var aggregateFactory = Mock.Of<IAggregateFactory>();
			var resolver = Mock.Of<IResolver>();
			var metadataProviders = Enumerable.Empty<IMetadataProvider>();
			var snapshotStore = Mock.Of<ISnapshotStore>();
			var log = new NullLog();
			var factory = new DomainEventFactory();
			var persistence = new InMemoryEventPersistence(log);
			var upgradeManager = new EventUpgradeManager(log, resolver);
			var definitionService = new EventDefinitionService(log);
			definitionService.Load(typeof(FirstTestEvent), typeof(SecondTestEvent));
			var serializer = new EventJsonSerializer(new JsonSerializer(), definitionService, factory);

			var store = new EventStoreBase(log, aggregateFactory,
				serializer, upgradeManager, metadataProviders,
				persistence, snapshotStore);

			return store;
		}


		// This does NOT use the Identity<>-class, which prefixes each value
		// with the class name. That means these identity values are not
		// compartmentalized to the ID-spaces of this aggregate, but are
		// rather global and non-unique.
		private class FirstAggregateId : IIdentity
		{
			public FirstAggregateId(string value)
			{
				Value = value;
			}

			public string Value { get; }
		}


		// Since these identity values are not namespaced,
		// they risk coming in conflict with FirstAggregateId.
		private class SecondAggregateId : IIdentity
		{
			public SecondAggregateId(string value)
			{
				Value = value;
			}

			public string Value { get; }
		}


		private class FirstAggregate : AggregateRoot<FirstAggregate, FirstAggregateId>
		{
			public FirstAggregate(FirstAggregateId id)
				: base(id)
			{}
		}


		private class SecondAggregate : AggregateRoot<SecondAggregate, SecondAggregateId>
		{
			public SecondAggregate(SecondAggregateId id)
				: base(id)
			{}
		}


		private class FirstTestEvent : AggregateEvent<FirstAggregate, FirstAggregateId>
		{}


		private class SecondTestEvent : AggregateEvent<SecondAggregate, SecondAggregateId>
		{}
	}
}