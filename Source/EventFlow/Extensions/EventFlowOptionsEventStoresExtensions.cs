﻿// The MIT License (MIT)
// 
// Copyright (c) 2015-2018 Rasmus Mikkelsen
// Copyright (c) 2015-2018 eBay Software Foundation
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
using System.Collections;
using EventFlow.Configuration;
using EventFlow.EventStores;
using EventFlow.EventStores.Files;

namespace EventFlow.Extensions
{
    public static class EventFlowOptionsEventStoresExtensions
    {
        public static IEventFlowOptions UseEventStore(
            this IEventFlowOptions eventFlowOptions,
            Func<IResolverContext, IEventStore> eventStoreResolver,
            Lifetime lifetime = Lifetime.AlwaysUnique)
        {
            return eventFlowOptions.RegisterServices(f => f.Register(eventStoreResolver, lifetime));
        }

        public static IEventFlowOptions UseEventStore<TEventStore>(
            this IEventFlowOptions eventFlowOptions,
            Lifetime lifetime = Lifetime.AlwaysUnique)
            where TEventStore : class, IEventPersistence
        {
            return eventFlowOptions.RegisterServices(f => f.Register<IEventPersistence, TEventStore>(lifetime));
        }

        public static IEventFlowOptions UseEventStore<TEventStore, TSerialized>(
            this IEventFlowOptions eventFlowOptions,
            Lifetime lifetime = Lifetime.AlwaysUnique)
            where TEventStore : class, IEventPersistence<TSerialized>
            where TSerialized : IEnumerable
        {
            return eventFlowOptions.RegisterServices(f => f.Register<IEventPersistence<TSerialized>, TEventStore>(lifetime));
        }

        public static IEventFlowOptions UseFilesEventStore(
            this IEventFlowOptions eventFlowOptions,
            IFilesEventStoreConfiguration filesEventStoreConfiguration)
        {
            return eventFlowOptions.RegisterServices(f =>
                {
                    f.Register(_ => filesEventStoreConfiguration, Lifetime.Singleton);
                    f.Register<IEventPersistence, FilesEventPersistence>(Lifetime.Singleton);
                    f.Register<IFilesEventLocator, FilesEventLocator>();
                });
        }
    }
}