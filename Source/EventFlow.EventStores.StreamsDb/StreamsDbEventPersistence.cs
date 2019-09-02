using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Exceptions;
using EventFlow.Logs;
using StreamsDB.Driver;

namespace EventFlow.EventStores.StreamsDb
{
	public class StreamsDbEventPersistence : IEventPersistence
	{
		private readonly ILog _log;
		private readonly StreamsDBClient _client;

		private class EventFlowEvent : ICommittedDomainEvent
		{
			public string AggregateId { get; set; }
			public string Data { get; set; }
			public string Metadata { get; set; }
			public int AggregateSequenceNumber { get; set; }
		}

		public StreamsDbEventPersistence(ILog log, StreamsDBClient client)
		{
			_log = log;
			_client = client;
		}

		public async Task<AllCommittedEventsPage> LoadAllCommittedEvents(GlobalPosition globalPosition, int pageSize, CancellationToken cancellationToken)
		{
			var streamsDbMessages = new List<Message>();

			IGlobalSlice currentSlice;
			var from = globalPosition.IsStart
				? StreamsDB.Driver.GlobalPosition.Begin
				: StreamsDB.Driver.GlobalPosition.Begin.Parse(globalPosition.Value);

			do
			{
				currentSlice = await _client.DB().ReadGlobalForward(from, pageSize).ConfigureAwait(false);
				from = currentSlice.Next;
				streamsDbMessages.AddRange(currentSlice.Messages);
			}
			while (streamsDbMessages.Count < pageSize && currentSlice.HasNext);

			var eventFlowEvents = Map(streamsDbMessages);

			return new AllCommittedEventsPage(new GlobalPosition(currentSlice.Next.ToString()), eventFlowEvents);
		}

		public async Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync(IIdentity id, IReadOnlyCollection<SerializedEvent> serializedEvents, CancellationToken cancellationToken)
		{
			var eventFlowEvents = serializedEvents
				.Select(e => new EventFlowEvent
				{
					AggregateSequenceNumber = e.AggregateSequenceNumber,
					Metadata = e.SerializedMetadata,
					AggregateId = id.Value,
					Data = e.SerializedData
				})
				.ToList();

			var expectedVersion = serializedEvents.Min(e => e.AggregateSequenceNumber) - 1;

			var streamsDbMessages = serializedEvents
				.Select(e =>
				{
					var eventId = e.Metadata.EventId.Value;
					var eventType = string.Format("{0}.{1}.{2}", e.Metadata[MetadataKeys.AggregateName], e.Metadata.EventName, e.Metadata.EventVersion);
					var data = Encoding.UTF8.GetBytes(e.SerializedData);
					var header = Encoding.UTF8.GetBytes(e.SerializedMetadata);

					return new MessageInput
					{
						ID = eventId,
						Type = eventType,
						Header = header,
						Value = data
					};
				})
				.ToList();

			try
			{
				await _client.DB().AppendStream(id.Value, ConcurrencyCheck.ExpectStreamVersion(expectedVersion), streamsDbMessages).ConfigureAwait(false);
			}
			catch (OperationCanceledException e)
			{
				throw new OptimisticConcurrencyException(e.Message, e);
			}

			return eventFlowEvents;
		}

		public async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(IIdentity id, int fromEventSequenceNumber, CancellationToken cancellationToken)
		{
			var streamEvents = new List<Message>();

			IStreamSlice currentSlice;

			do
			{
                currentSlice = await _client.DB().ReadStreamForward(id.Value, fromEventSequenceNumber, int.MaxValue).ConfigureAwait(false);
				fromEventSequenceNumber = (int)currentSlice.Next;
				streamEvents.AddRange(currentSlice.Messages);
			}
			while (currentSlice.HasNext);

			return Map(streamEvents);
		}

		public Task DeleteEventsAsync(IIdentity id, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}

		private static IReadOnlyCollection<EventFlowEvent> Map(IEnumerable<Message> resolvedEvents)
		{
			return resolvedEvents
				.Select(e => new EventFlowEvent
				{
					AggregateSequenceNumber = (int)e.Position,
					Metadata = Encoding.UTF8.GetString(e.Header),
					AggregateId = e.Stream,
					Data = Encoding.UTF8.GetString(e.Value),
				})
				.ToList();
		}
	}
}