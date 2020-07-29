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
using EventFlow.Extensions;

namespace EventFlow.Sagas
{
    public class SagaDetails
    {
        public static SagaDetails From<T>()
            where T : ISaga
        {
            return From(typeof(T));
        }

        public static SagaDetails From(Type sagaType)
        {
            if (!typeof(ISaga).GetTypeInfo().IsAssignableFrom(sagaType))
            {
                throw new ArgumentException(
                    $"Type {sagaType.PrettyPrint()} is not a {typeof(ISaga).PrettyPrint()}",
                    nameof(sagaType));
            }

            var sagaInterfaces = sagaType
                .GetTypeInfo()
                .GetInterfaces()
                .Select(i => i.GetTypeInfo())
                .ToList();
            var sagaHandlesTypes = sagaInterfaces
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISagaHandles<,,>))
                .ToList();
            var sagaStartedByTypes = sagaInterfaces
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISagaIsStartedBy<,,>))
                .Select(i => i.GetGenericArguments()[2])
                .ToList();
            var aggregateEventTypes = sagaHandlesTypes
                .Select(i => i.GetGenericArguments()[2])
                .ToList();
            var sagaInterfaceType = sagaInterfaces.Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISaga<>));

            var sagaTypeDetails = new SagaDetails(
                sagaType,
                sagaInterfaceType.GetGenericArguments()[0],
                sagaStartedByTypes,
                aggregateEventTypes);

            return sagaTypeDetails;
        }

        private readonly ISet<Type> _startedBy;

        private SagaDetails(
            Type sagaType,
            Type sagaLocatorType,
            IEnumerable<Type> startedBy,
            IReadOnlyCollection<Type> aggregateEventTypes)
        {
            _startedBy = new HashSet<Type>(startedBy);

            SagaType = sagaType;
            SagaLocatorType = sagaLocatorType;
            AggregateEventTypes = aggregateEventTypes;
        }

        public Type SagaType { get; }
        public Type SagaLocatorType { get; }
        public IReadOnlyCollection<Type> AggregateEventTypes { get; }

        public bool IsStartedBy(Type aggregateEventType)
        {
            return _startedBy.Contains(aggregateEventType);
        }
    }
}