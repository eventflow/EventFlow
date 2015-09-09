﻿// The MIT License (MIT)
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
using System.Reflection;
using EventFlow.Commands;

namespace EventFlow.Extensions
{
    public static class EventFlowOptionsCommandExtensions
    {
        public static EventFlowOptions AddCommandHandlers(
            this EventFlowOptions eventFlowOptions,
            Assembly fromAssembly,
            Predicate<Type> predicate = null)
        {
            predicate = predicate ?? (t => true);
            var commandHandlerTypes = fromAssembly
                .GetTypes()
                .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (ICommandHandler<,,>)))
                .Where(t => predicate(t));
            return eventFlowOptions.AddCommandHandlers(commandHandlerTypes);
        }

        public static EventFlowOptions AddCommandHandlers(
            this EventFlowOptions eventFlowOptions,
            params Type[] commandHandlerTypes)
        {
            return eventFlowOptions.AddCommandHandlers((IEnumerable<Type>) commandHandlerTypes);
        }

        public static EventFlowOptions AddCommandHandlers(
            this EventFlowOptions eventFlowOptions,
            IEnumerable<Type> commandHandlerTypes)
        {
            foreach (var commandHandlerType in commandHandlerTypes)
            {
                var t = commandHandlerType;
                var handlesCommandTypes = t
                    .GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (ICommandHandler<,,>))
                    .ToList();
                if (!handlesCommandTypes.Any())
                {
                    throw new ArgumentException(string.Format(
                        "Type '{0}' does not implement ICommandHandler<TAggregate, TCommand>",
                        commandHandlerType.Name));
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
    }
}
