// The MIT License (MIT)
//
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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
using EventFlow.Configuration;
using EventFlow.Extensions;
using KurrentDB.Client;

namespace EventFlow.EventStores.KurrentDB.Extensions
{
    public static class EventFlowOptionsExtensions
    {
        /// <summary>
        /// Uses KurrentDB for event persistence. Each root resolver owns one
        /// client, shared by all of its scopes and disposed with the resolver.
        /// </summary>
        public static IEventFlowOptions UseKurrentDBEventStore(
            this IEventFlowOptions eventFlowOptions,
            string connectionString)
        {
            if (eventFlowOptions == null)
            {
                throw new ArgumentNullException(nameof(eventFlowOptions));
            }
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("A KurrentDB connection string is required.", nameof(connectionString));
            }

            // Validate configuration now; the client is created and owned by DI.
            var settings = KurrentDBClientSettings.Create(connectionString);
            return eventFlowOptions
                .RegisterServices(services => services.Register(
                    _ => new KurrentDBClient(settings), Lifetime.Singleton))
                .UseEventStore<KurrentDBEventPersistence>();
        }
    }
}
