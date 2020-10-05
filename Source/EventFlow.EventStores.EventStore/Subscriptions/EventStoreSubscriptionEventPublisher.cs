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

using EventFlow.Aggregates;
using EventFlow.Logs;
using EventFlow.Subscribers;
using EventStore.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.EventStores.EventStore.Subscriptions
{
    public class EventStoreSubscriptionEventPublisher
    {
        private readonly EventStoreClient _client;
        private StreamSubscription _subscription;
        private readonly IEventFilter _eventFilter;
        private readonly uint _checkpointInterval;
        private readonly IEventStoreCheckpointStore _checkpointStore;
        private readonly IEventJsonSerializer _eventJsonSerializer;
        private readonly IDomainEventPublisher _domainEventPublisher;
        private readonly ILog _log;

        public EventStoreSubscriptionEventPublisher(
            ILog log,
            EventStoreClient client,
            IEventFilter eventFilter,
            uint checkpointInterval,
            IEventStoreCheckpointStore checkpointStore,
            IEventJsonSerializer eventJsonSerializer,
            IDomainEventPublisher domainEventPublisher)
        {
            _log = log;
            _client = client;
            _checkpointStore = checkpointStore;
            _eventFilter = eventFilter;
            _checkpointInterval = checkpointInterval;
            _eventJsonSerializer = eventJsonSerializer;
            _domainEventPublisher = domainEventPublisher;
        }

        public async Task StartAsync()
        {
            var position = await _checkpointStore.GetCheckpoint();
            
            _log.Information($"Starting subscription on stream '$all' at checkpoint '{(position ?? 0)}' listening for events matching '{_eventFilter.ToString()}' ");

            _subscription = await _client.SubscribeToAllAsync(
                    position.HasValue 
                        ? new Position(position.Value, position.Value) 
                        : Position.Start,
                    EventReceivedAsync,
                    filterOptions: new SubscriptionFilterOptions(
                        _eventFilter,
                        checkpointInterval: _checkpointInterval,
                        checkpointReached: CheckpointReached
                    ),
                    subscriptionDropped: SubscriptionDropped,
                    resolveLinkTos: true);
        }

        private async Task EventReceivedAsync(StreamSubscription _, ResolvedEvent resolvedEvent, CancellationToken c)
        {
            try
            {
                var eventStoreEvent = new EventStoreEvent
                {
                    AggregateId = resolvedEvent.Event.EventStreamId,
                    AggregateSequenceNumber = (int)resolvedEvent.Event.EventNumber.ToInt64(),
                    Metadata = Encoding.UTF8.GetString(resolvedEvent.Event.Metadata.Span),
                    Data = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span),
                };

                var deserializedEventStoreEvent = _eventJsonSerializer.Deserialize(eventStoreEvent);

                await _domainEventPublisher.PublishAsync(new List<IDomainEvent> { deserializedEventStoreEvent }, new CancellationToken());
            }
            catch(Exception e)
            {
                _log.Error(e, $"Error while processing event '{resolvedEvent.Event.EventId}'");
            }
        }

        private Task CheckpointReached(StreamSubscription _, Position position, CancellationToken c)
        {
            _checkpointStore.StoreCheckpoint(position.PreparePosition);

            return Task.CompletedTask;
        }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        private void SubscriptionDropped(StreamSubscription _, SubscriptionDroppedReason reason, Exception? c)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        {
            _log.Information($"Dropping subscription on stream '$all' for the following reason: {reason}");
        }

        public void Stop()
        {
            _log.Information($"Stopping subscription on stream '$all'");

            _subscription.Dispose();
        }
    }
}