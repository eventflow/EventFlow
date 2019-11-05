using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.EventStores.StreamsDb.Integration;
using EventFlow.Extensions;
using EventFlow.Logs;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Polly;
using StreamsDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.EventStores.StreamsDb.Integrations
{
	public class StreamsDbSubscriberManager : IHostedService
	{
		private readonly ILog _log;
		private readonly IResolver _resolver;
		private StreamsDBClient _client;
		private readonly string _stream;
		private readonly string _cursorsStream;

		private long _cursor;

		public StreamsDbSubscriberManager(
			ILog log,
			IResolver resolver,
			StreamsDBClient client,
			string stream,
			string subscriber)
		{
			_log = log;
			_resolver = resolver;
			_client = client;
			_stream = stream;
			_cursorsStream = $"{subscriber}-cursors";
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			await InitializeCursors();
			Subscribe();
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		private async Task InitializeCursors()
		{
			var (message, found) = await _client.DB().ReadLastMessageFromStream(_cursorsStream);
			_cursor = found ? JsonConvert.DeserializeObject<long>(Encoding.UTF8.GetString(message.Value)) : 0;
		}

		private void Subscribe()
		{
			Task.Run(async () =>
			{
				var subscription = _client.DB().SubscribeStream(_stream, _cursor + 1);

				var retryPolicy = Policy
				 .Handle<Exception>()
				 .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

				do
				{
					await subscription.MoveNext().ConfigureAwait(false);

					await retryPolicy.ExecuteAsync(async () =>
					{
						var eventJson = Encoding.UTF8.GetString(subscription.Current.Value);
						var metadataJson = Encoding.UTF8.GetString(subscription.Current.Header);
						var metadata = JsonConvert.DeserializeObject<Metadata>(metadataJson);

						var integrationEvent = new IntegrationEvent(eventJson, metadata);

						await DispatchToSubscribersAsync(integrationEvent);

						// update cursor. This only works when the commands are idempotent
						// eg. use ISourceId, or an Idempotent Producer
						var cursorsMessageInput = new MessageInput
						{
							ID = Guid.NewGuid().ToString(),
							Type = "cursors",
							Value = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_cursor))
						};

						await _client.DB().AppendStream(_cursorsStream, cursorsMessageInput);

						_cursor = subscription.Current.Position;
					});				
				}
				while (true);
			});
		}

		private async Task DispatchToSubscribersAsync(IntegrationEvent integrationEvent)
		{
			var subscribers = _resolver
				.ResolveAll(typeof(IStreamsDbSubscriber))
				.Cast<IStreamsDbSubscriber>()
				.ToList();

			if (!subscribers.Any())
			{
				_log.Debug(() => $"Didn't find any subscribers to '{integrationEvent.Metadata.EventName}'");
			}

			foreach (var subscriber in subscribers)
			{
				_log.Verbose(() => $"Calling HandleAsync on handler '{subscriber.GetType().PrettyPrint()}' " +
								   $"for aggregate event '{integrationEvent.Metadata.EventName}'");

				try
				{
					var executionResult = await subscriber.HandleAsync(integrationEvent);

					if (!executionResult.IsSuccess)
					{
						throw new Exception("Execution is not succesfull");
					}
				}
				catch (Exception e)
				{
					_log.Error(e, $"Subscriber '{subscriber.GetType().PrettyPrint()}' threw " +
								  $"'{e.GetType().PrettyPrint()}' while handling '{integrationEvent.Metadata.EventName}': {e.Message}");
					throw;
				}
			}
		}
	}
}