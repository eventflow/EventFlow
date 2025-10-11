// The MIT License (MIT)
// 
// Copyright (c) 2015-2025 Rasmus Mikkelsen
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
using EventFlow.Extensions;
using KurrentDB.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventFlow.Kurrent.Extensions
{
    public static class EventFlowOptionsExtensions
    {
        public static IEventFlowOptions UseKurrentEventStore(this IEventFlowOptions eventFlowOptions, string connectionString)
        {
            if (eventFlowOptions == null) throw new ArgumentNullException(nameof(eventFlowOptions));
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            var settings = KurrentDBClientSettings.Create(connectionString);
            return eventFlowOptions.UseKurrentEventStore(settings);
        }

        public static IEventFlowOptions UseKurrentEventStore(this IEventFlowOptions eventFlowOptions, KurrentDBClientSettings settings)
        {
            if (eventFlowOptions == null) throw new ArgumentNullException(nameof(eventFlowOptions));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            eventFlowOptions.ServiceCollection.RemoveAll<KurrentDBClient>();
            eventFlowOptions.ServiceCollection.TryAddSingleton(_ => new KurrentDBClient(settings));

            return eventFlowOptions.UseEventPersistence<KurrentEventPersistence>(ServiceLifetime.Singleton);
        }

        public static IEventFlowOptions UseKurrentEventStore(this IEventFlowOptions eventFlowOptions, Func<IServiceProvider, KurrentDBClient> clientFactory)
        {
            if (eventFlowOptions == null) throw new ArgumentNullException(nameof(eventFlowOptions));
            if (clientFactory == null) throw new ArgumentNullException(nameof(clientFactory));

            eventFlowOptions.ServiceCollection.RemoveAll<KurrentDBClient>();
            eventFlowOptions.ServiceCollection.AddSingleton(clientFactory);

            return eventFlowOptions.UseEventPersistence<KurrentEventPersistence>(ServiceLifetime.Singleton);
        }
    }
}
