// The MIT License (MIT)
//
// Copyright (c) 2015 EventFlow
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
using Common.Logging;
using Common.Logging.Simple;
using EventFlow.EventStores;

namespace EventFlow.Configuration
{
    internal class AutofacInitialization
    {
        public static IContainer Configure(EventFlowOptions options)
        {
            var regs = options.GetRegistrations().ToDictionary(r => r.ServiceType, r => r);
            
            Default(regs, new DiRegistration<ILog>(r => new ConsoleOutLogger("EventFlow", LogLevel.Debug, true, true, false, "HH:mm:ss")));
            Default(regs, new DiRegistration<IEventStore, InMemoryEventStore>(Lifetime.Singleton));
            Default(regs, new DiRegistration<ICommandBus, CommandBus>());
            Default(regs, new DiRegistration<IEventDefinitionService, EventDefinitionService>());
            Default(regs, new DiRegistration<IDispatchToEventHandlers, DispatchToEventHandlers>());

            var containerBuilder = new ContainerBuilder();
            foreach (var reg in regs.Values)
            {
                reg.Configure(containerBuilder);
            }

            return containerBuilder.Build();
        }

        private static void Default(IDictionary<Type, DiRegistration> registrations, DiRegistration defaultRegistration)
        {
            if (registrations.ContainsKey(defaultRegistration.ServiceType))
            {
                return;
            }
            registrations.Add(defaultRegistration.ServiceType, defaultRegistration);
        }
    }
}
