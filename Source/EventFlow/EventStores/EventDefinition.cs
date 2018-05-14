// The MIT License (MIT)
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
using System.Linq;
using System.Reflection;
using EventFlow.Aggregates;
using EventFlow.Core.VersionedTypes;

namespace EventFlow.EventStores
{
    public class EventDefinition : VersionedTypeDefinition
    {
        private readonly Lazy<Type> _lazyAggregateType;
        private readonly Lazy<Type> _lazyIdentityType;
        
        public Type AggregateType => _lazyAggregateType.Value;
        public Type IdentityType => _lazyIdentityType.Value;
        
        public EventDefinition(
            int version,
            Type type,
            string name)
            : base(version, type, name)
        {
            var lazyAggregateEventType = new Lazy<Type>(() => GetAggregateEventType(type));
            
            _lazyAggregateType = new Lazy<Type>(() => lazyAggregateEventType.Value.GenericTypeArguments[0]);
            _lazyIdentityType = new Lazy<Type>(() => lazyAggregateEventType.Value.GenericTypeArguments[1]);
        }

        private static Type GetAggregateEventType(Type eventType)
        {
            return eventType
                .GetTypeInfo()
                .GetInterfaces()
                .Single(i =>
                    {
                        var iti = i.GetTypeInfo();
                        return iti.IsGenericType && iti.GetGenericTypeDefinition() == typeof(IAggregateEvent<,>);
                    });
        }
    }
}