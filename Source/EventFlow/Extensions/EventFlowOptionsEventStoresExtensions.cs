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
using EventFlow.EventStores;
using EventFlow.EventStores.Files;
using Microsoft.Extensions.DependencyInjection;

namespace EventFlow.Extensions
{
    public static class EventFlowOptionsEventStoresExtensions
    {
        public static IEventFlowOptions UseEventPersistence(
            this IEventFlowOptions eventFlowOptions,
            Func<IServiceProvider, IEventPersistence> eventPersistenceResolver,
            ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            eventFlowOptions.ServiceCollection
                .Add(new ServiceDescriptor(typeof(IEventStore), eventPersistenceResolver, serviceLifetime));
            return eventFlowOptions;
        }

        public static IEventFlowOptions UseEventPersistence<TEventPersistence>(
            this IEventFlowOptions eventFlowOptions,
            ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TEventPersistence : class, IEventPersistence
        {
            eventFlowOptions.ServiceCollection
                .Add(new ServiceDescriptor(typeof(IEventPersistence), typeof(TEventPersistence), serviceLifetime));
            return eventFlowOptions;
        }

        public static IEventFlowOptions UseFilesEventPersistence(
            this IEventFlowOptions eventFlowOptions,
            IFilesEventStoreConfiguration filesEventStoreConfiguration)
        {
            eventFlowOptions.ServiceCollection
                .AddSingleton(filesEventStoreConfiguration)
                .AddSingleton<IEventPersistence, FilesEventPersistence>()
                .AddTransient<IFilesEventLocator, FilesEventLocator>();
            return eventFlowOptions;
        }
    }
}