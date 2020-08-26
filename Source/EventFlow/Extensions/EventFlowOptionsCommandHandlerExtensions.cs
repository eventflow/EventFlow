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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EventFlow.Commands;

namespace EventFlow.Extensions
{
    public static class EventFlowOptionsCommandHandlerExtensions
    {
        public static IEventFlowOptions AddCommandHandlers(
            this IEventFlowOptions eventFlowOptions,
            Assembly fromAssembly,
            Predicate<Type> predicate = null)
        {
            predicate = predicate ?? (t => true);
            var commandHandlerTypes = fromAssembly
                .GetTypes()
                .Where(t => t.GetTypeInfo().GetInterfaces().Any(IsCommandHandlerInterface))
                .Where(t => !t.HasConstructorParameterOfType(IsCommandHandlerInterface))
                .Where(t => predicate(t));
            return eventFlowOptions.AddCommandHandlers(commandHandlerTypes);
        }

        public static IEventFlowOptions AddCommandHandlers(
            this IEventFlowOptions eventFlowOptions,
            params Type[] commandHandlerTypes)
        {
            return eventFlowOptions.AddCommandHandlers((IEnumerable<Type>) commandHandlerTypes);
        }

        public static IEventFlowOptions AddCommandHandlers(
            this IEventFlowOptions eventFlowOptions,
            IEnumerable<Type> commandHandlerTypes)
        {
            foreach (var commandHandlerType in commandHandlerTypes)
            {
                var t = commandHandlerType;
                if (t.GetTypeInfo().IsAbstract) continue;
                var handlesCommandTypes = t
                    .GetTypeInfo()
                    .GetInterfaces()
                    .Where(IsCommandHandlerInterface)
                    .ToList();
                if (!handlesCommandTypes.Any())
                {
                    throw new ArgumentException($"Type '{commandHandlerType.PrettyPrint()}' does not implement '{typeof(ICommandHandler<,,,>).PrettyPrint()}'");
                }

                eventFlowOptions.RegisterServices(sr =>
                    {
                        foreach (var handlesCommandType in handlesCommandTypes)
                        {
                            sr.Register(handlesCommandType, t);
                        }
                    });
            }

            return eventFlowOptions;
        }

        private static bool IsCommandHandlerInterface(this Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(ICommandHandler<,,,>);
        }
    }
}
