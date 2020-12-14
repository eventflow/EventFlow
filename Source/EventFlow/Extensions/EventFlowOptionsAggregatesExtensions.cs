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
using System.Reflection;

namespace EventFlow.Extensions
{
    public static class EventFlowOptionsAggregatesExtensions
    {
        [Obsolete("Resolver aggregate factory is the default, simply remove this call")]
        public static IEventFlowOptions UseResolverAggregateRootFactory(
            this IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions;
        }

        [Obsolete("Default aggregate factory doesn't require aggregate roots to be registered, simply remove this call")]
        public static IEventFlowOptions AddAggregateRoots(
            this IEventFlowOptions eventFlowOptions,
            Assembly fromAssembly,
            Predicate<Type> predicate = null)
        {
            return eventFlowOptions;
        }

        [Obsolete("Default aggregate factory doesn't require aggregate roots to be registered, simply remove this call")]
        public static IEventFlowOptions AddAggregateRoots(
            this IEventFlowOptions eventFlowOptions,
            params Type[] aggregateRootTypes)
        {
            return eventFlowOptions;
        }

        [Obsolete("Default aggregate factory doesn't require aggregate roots to be registered, simply remove this call")]
        public static IEventFlowOptions AddAggregateRoots(
            this IEventFlowOptions eventFlowOptions,
            IEnumerable<Type> aggregateRootTypes)
        {
            return eventFlowOptions;
        }
    }
}