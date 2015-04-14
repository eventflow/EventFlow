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
using Autofac;
using Autofac.Core;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Configuration.Resolvers;

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

        public IRootResolver CreateResolver(bool validateRegistrations = true)
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
