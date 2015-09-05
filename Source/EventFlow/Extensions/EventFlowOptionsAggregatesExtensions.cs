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
using Autofac;
using EventFlow.Aggregates;
using EventFlow.Configuration.Registrations.Services;

namespace EventFlow.Extensions
{
    public static class EventFlowOptionsAggregatesExtensions
    {
        public static EventFlowOptions UseResolverAggregateRootFactory(
            this EventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions.RegisterServices(f => f.Register<IAggregateFactory, AutofacAggregateRootFactory>());
        }

        public static EventFlowOptions AddAggregateRoots(
            this EventFlowOptions eventFlowOptions,
            Assembly fromAssembly)
        {
            var aggregateRootTypes = fromAssembly
                .GetTypes()
                .Where(t => !t.IsAbstract)
                .Where(t => t.IsClosedTypeOf(typeof(IAggregateRoot<>)));
            eventFlowOptions.AddAggregateRoots(aggregateRootTypes);
            return eventFlowOptions;
        }

        public static EventFlowOptions AddAggregateRoots(
            this EventFlowOptions eventFlowOptions,
            params Type[] aggregateRootTypes)
        {
            eventFlowOptions.AddAggregateRoots((IEnumerable<Type>)aggregateRootTypes);
            return eventFlowOptions;
        }

        public static EventFlowOptions AddAggregateRoots(
            this EventFlowOptions eventFlowOptions,
            IEnumerable<Type> aggregateRootTypes)
        {
            var invalidTypes = aggregateRootTypes
                .Where(t => t.IsAbstract || !t.IsClosedTypeOf(typeof(IAggregateRoot<>)))
                .ToList();
            if (invalidTypes.Any())
            {
                var names = string.Join(", ", invalidTypes.Select(t => t.Name));
                throw new ArgumentException($"Type(s) '{names}' do not implement IAggregateRoot<TIdentity>");
            }

            foreach (var t in aggregateRootTypes)
            {
                eventFlowOptions.RegisterServices(sr => sr.RegisterType(t));
            }

            return eventFlowOptions;
        }
    }
}
