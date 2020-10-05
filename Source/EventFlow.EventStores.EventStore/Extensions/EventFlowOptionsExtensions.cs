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
using EventFlow.Configuration;
using EventFlow.EventStores.EventStore.Subscriptions;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.Subscribers;
using EventStore.Client;
using AggregateStore = EventFlow.EventStores.EventStore.Aggregates.AggregateStore;

namespace EventFlow.EventStores.EventStore.Extensions
{
    public static class EventFlowOptionsExtensions
    {
        public static IEventFlowOptions UseEventStoreEventStore(
            this IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions.UseEventStore<EventStoreEventPersistence>();
        }

        public static IEventFlowOptions UseEventStoreEventStore(
            this IEventFlowOptions eventFlowOptions,
            EventStoreClientSettings eventStoreClientSettings)
        {
            var eventStoreClient = new EventStoreClient(eventStoreClientSettings);

            return eventFlowOptions
                .RegisterServices(sr => sr.Register(r => eventStoreClient, Lifetime.Singleton))
                .UseEventStore<EventStoreEventPersistence>();
        }

        public static IEventFlowOptions UseEventStoreSubscriptions(
            this IEventFlowOptions eventFlowOptions,
            string subscriptionName,
            IEventFilter eventFilter,
            uint checkpointInterval = 10)
        {
            eventFlowOptions.RegisterServices(sr => sr.Register<IAggregateStore, AggregateStore>());

            var resolver = eventFlowOptions.CreateResolver();

            var eventStoreClient = resolver.Resolve<EventStoreClient>();

            var eventStoreCheckpointStore = new EventStoreCheckpointStore(eventStoreClient, subscriptionName);

            var eventStoreSubscriptionEventPublisher = new EventStoreSubscriptionEventPublisher(
                resolver.Resolve<ILog>(),
                eventStoreClient,
                eventFilter,
                checkpointInterval,
                eventStoreCheckpointStore,
                resolver.Resolve<IEventJsonSerializer>(),
                resolver.Resolve<IDomainEventPublisher>()
            );

            eventStoreSubscriptionEventPublisher.StartAsync().Wait();

            return eventFlowOptions;
        }
    }
}