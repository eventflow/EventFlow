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
using System.Collections.Generic;
using System.Linq;
using Autofac;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventCaches;
using EventFlow.EventCaches.InMemory;
using EventFlow.EventStores;
using EventFlow.EventStores.InMemory;
using EventFlow.Logs;
using EventFlow.ReadStores;
using EventFlow.Subscribers;

namespace EventFlow.Configuration.Resolvers
{
    internal class AutofacInitialization
    {
        public static IContainer Configure(EventFlowOptions options)
        {
            var regs = options.GetRegistrations()
                .GroupBy(r => r.ServiceType)
                .ToDictionary(g => g.Key, g => g.ToList());
            
            Check(regs, new Registration<ILog, ConsoleLog>(), false);
            Check(regs, new Registration<IEventStore, InMemoryEventStore>(Lifetime.Singleton), false);
            Check(regs, new Registration<ICommandBus, CommandBus>(), false);
            Check(regs, new Registration<IDispatchToEventSubscribers, DispatchToEventSubscribers>(), false);
            Check(regs, new Registration<IEventJsonSerializer, EventJsonSerializer>(), false);
            Check(regs, new Registration<IEventDefinitionService, EventDefinitionService>(Lifetime.Singleton), false);
            Check(regs, new Registration<IReadStoreManager, ReadStoreManager>(), false);
            Check(regs, new Registration<IJsonSerializer, JsonSerializer>(), false);
            Check(regs, new Registration<IAggregateFactory, AggregateFactory>(), false);
            Check(regs, new Registration<IDomainEventPublisher, DomainEventPublisher>(), false);
            Check(regs, new Registration<IDomainEventFactory, DomainEventFactory>(Lifetime.Singleton), false);
            Check(regs, new Registration<IEventCache, InMemoryEventCache>(Lifetime.Singleton), false);

            var eventFlowConfiguration = options.GetEventFlowConfiguration();

            var containerBuilder = new ContainerBuilder();
            
            containerBuilder.Register(c => new AutofacResolver(c.Resolve<IComponentContext>())).As<IResolver>();
            containerBuilder.RegisterInstance(eventFlowConfiguration).As<IEventFlowConfiguration>().SingleInstance();
            foreach (var reg in regs.Values.SelectMany(r => r))
            {
                reg.Configure(containerBuilder);
            }

            var container = containerBuilder.Build();

            var eventDefinitionService = container.Resolve<IEventDefinitionService>();
            eventDefinitionService.LoadEvents(options.GetAggregateEventTypes());

            return container;
        }

        private static void Check(IDictionary<Type, List<Registration>> registrations, Registration defaultRegistration, bool allowMultiple)
        {
            if (!registrations.ContainsKey(defaultRegistration.ServiceType))
            {
                registrations.Add(defaultRegistration.ServiceType, new List<Registration>{defaultRegistration});
                return;
            }

            if (allowMultiple)
            {
                return;
            }

            var serviceRegrations = registrations[defaultRegistration.ServiceType];
            if (serviceRegrations.Count > 1)
            {
                throw new InvalidOperationException(string.Format(
                    "You may make one registration of {0}, however these are registered: {1}",
                    defaultRegistration.ServiceType.Name,
                    string.Join(", ", serviceRegrations.Select(r => r.ToString()))));
            }
        }
    }
}
