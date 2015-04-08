// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
// https://github.com/rasmus/EventFlow
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Core;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Configuration.Resolvers;
using EventFlow.EventStores;
using EventFlow.ReadStores;
using EventFlow.ReadStores.InMemory;

namespace EventFlow
{
    public class EventFlowOptions
    {
        public static EventFlowOptions New { get { return new EventFlowOptions(); } }

        private readonly ConcurrentBag<Registration> _registrations = new ConcurrentBag<Registration>();
        private readonly ConcurrentBag<Type> _aggregateEventTypes = new ConcurrentBag<Type>();
        private readonly EventFlowConfiguration _eventFlowConfiguration = new EventFlowConfiguration();

        private EventFlowOptions() { }

        public EventFlowOptions ConfigureOptimisticConcurrentcyRetry(int retries, TimeSpan delayBeforeRetry)
        {
            _eventFlowConfiguration.NumberOfRetriesOnOptimisticConcurrencyExceptions = retries;
            _eventFlowConfiguration.DelayBeforeRetryOnOptimisticConcurrencyExceptions = delayBeforeRetry;
            return this;
        }

        public EventFlowOptions UseEventStore(Func<IResolver, IEventStore> eventStoreResolver, Lifetime lifetime = Lifetime.AlwaysUnique)
        {
            AddRegistration(new Registration<IEventStore>(eventStoreResolver, lifetime));
            return this;
        }

        public EventFlowOptions UseEventStore<TEventStore>(Lifetime lifetime = Lifetime.AlwaysUnique)
            where TEventStore : class, IEventStore
        {
            AddRegistration(new Registration<IEventStore, TEventStore>(lifetime));
            return this;
        }

        public EventFlowOptions UseInMemoryReadStoreFor<TAggregate, TReadModel>()
            where TAggregate : IAggregateRoot
            where TReadModel : IReadModel, new()
        {
            AddReadModelStore<TAggregate, IInMemoryReadModelStore<TAggregate, TReadModel>>();
            AddRegistration(new Registration<IInMemoryReadModelStore<TAggregate, TReadModel>, InMemoryReadModelStore<TAggregate, TReadModel>>(Lifetime.Singleton));
            return this;
        }

        public EventFlowOptions AddMetadataProvider<TMetadataProvider>(Lifetime lifetime = Lifetime.AlwaysUnique)
            where TMetadataProvider : class, IMetadataProvider
        {
            AddRegistration(new Registration<IMetadataProvider, TMetadataProvider>(lifetime));
            return this;
        }

        public EventFlowOptions AddReadModelStore<TAggregate, TReadModelStore>(Lifetime lifetime = Lifetime.AlwaysUnique)
            where TAggregate : IAggregateRoot
            where TReadModelStore : class, IReadModelStore<TAggregate>
        {
            if (typeof(TReadModelStore).IsInterface)
            {
                AddRegistration(new Registration<IReadModelStore<TAggregate>>(r => r.Resolve<TReadModelStore>(), lifetime));
            }
            else
            {
                AddRegistration(new Registration<IReadModelStore<TAggregate>, TReadModelStore>(lifetime));
            }

            return this;
        }

        public EventFlowOptions AddEvents(Assembly fromAssembly)
        {
            var aggregateEventTypes = fromAssembly
                .GetTypes()
                .Where(t => !t.IsAbstract && typeof(IAggregateEvent).IsAssignableFrom(t));
            AddEvents(aggregateEventTypes);
            return this;
        }

        public EventFlowOptions AddEvents(params Type[] aggregateEventTypes)
        {
            AddEvents((IEnumerable<Type>)aggregateEventTypes);
            return this;
        }

        public EventFlowOptions AddEvents(IEnumerable<Type> aggregateEventTypes)
        {
            foreach (var aggregateEventType in aggregateEventTypes)
            {
                if (!typeof(IAggregateEvent).IsAssignableFrom(aggregateEventType))
                {
                    throw new ArgumentException(string.Format(
                        "Type {0} is not a {1}",
                        aggregateEventType.Name,
                        typeof(IAggregateEvent).Name));
                }
                _aggregateEventTypes.Add(aggregateEventType);
            }
            return this;
        }

        public EventFlowOptions AddRegistration(Registration registration)
        {
            _registrations.Add(registration);
            return this;
        }

        public bool HasRegistration<TService>()
        {
            var serviceType = typeof(TService);
            return _registrations.Any(r => r.ServiceType == serviceType);
        }

        internal IEnumerable<Registration> GetRegistrations()
        {
            return _registrations;
        }

        internal IEnumerable<Type> GetAggregateEventTypes()
        {
            return _aggregateEventTypes;
        }

        internal IEventFlowConfiguration GetEventFlowConfiguration()
        {
            return _eventFlowConfiguration;
        }

        public IRootResolver CreateResolver(bool validateRegistrations = false)
        {
            var container = AutofacInitialization.Configure(this);

            if (validateRegistrations)
            {
                var services = container
                    .ComponentRegistry
                    .Registrations
                    .SelectMany(x => x.Services)
                    .OfType<TypedService>()
                    .Where(x => !x.ServiceType.Name.StartsWith("Autofac"))
                    .ToList();
                var exceptions = new List<Exception>();
                foreach (var typedService in services)
                {
                    try
                    {
                        container.Resolve(typedService.ServiceType);
                    }
                    catch (DependencyResolutionException ex)
                    {
                        exceptions.Add(ex);
                    }
                }
                if (exceptions.Any())
                {
                    var message = string.Join(", ", exceptions.Select(e => e.Message));
                    throw new AggregateException(message, exceptions);
                }
            }

            return new AutofacRootResolver(container);
        }
    }
}
