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

using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Extensions;
using EventStore.ClientAPI;
using System;
using System.Net;

namespace EventFlow.EventStores.EventStore.Extensions
{
    public static class EventFlowOptionsExtensions
    {
        public static IEventFlowOptions UseEventStoreEventStore(
            this IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions.UseEventStore<EventStoreEventPersistence>();
        }

        [Obsolete("Use the overloads with 'uri' parameter instead.")]
        public static IEventFlowOptions UseEventStoreEventStore(
            this IEventFlowOptions eventFlowOptions,
            IPEndPoint ipEndPoint)
        {
            return eventFlowOptions
                .UseEventStoreEventStore(ipEndPoint, ConnectionSettings.Default);
        }

        [Obsolete("Use the overloads with 'uri' parameter instead.")]
        public static IEventFlowOptions UseEventStoreEventStore(
            this IEventFlowOptions eventFlowOptions,
            IPEndPoint ipEndPoint,
            ConnectionSettings connectionSettings)
        {
            var eventStoreConnection = EventStoreConnection.Create(
                connectionSettings,
                ipEndPoint,
                $"EventFlow v{typeof(EventFlowOptionsExtensions).Assembly.GetName().Version}");

            using (var a = AsyncHelper.Wait)
            {
                a.Run(eventStoreConnection.ConnectAsync());
            }

            return eventFlowOptions
                .RegisterServices(f => f.Register(r => eventStoreConnection, Lifetime.Singleton))
                .UseEventStore<EventStoreEventPersistence>();
        }

        public static IEventFlowOptions UseEventStoreEventStore(
            this IEventFlowOptions eventFlowOptions,
            Uri uri,
            ConnectionSettings connectionSettings,
            string connectionNamePrefix = null)
        {
            var sanitizedConnectionNamePrefix = string.IsNullOrEmpty(connectionNamePrefix)
                ? string.Empty
                : connectionNamePrefix + " - ";

            var eventStoreConnection = EventStoreConnection.Create(
                connectionSettings,
                uri,
                $"{sanitizedConnectionNamePrefix}EventFlow v{typeof(EventFlowOptionsExtensions).Assembly.GetName().Version}");

#pragma warning disable 618
            // TODO: Figure out bootstrapping alternative for 1.0
            using (var a = AsyncHelper.Wait)
            {
                a.Run(eventStoreConnection.ConnectAsync());
            }
#pragma warning restore 618

            return eventFlowOptions
                .RegisterServices(f => f.Register(r => eventStoreConnection, Lifetime.Singleton))
                .UseEventStore<EventStoreEventPersistence>();
        }
    }
}