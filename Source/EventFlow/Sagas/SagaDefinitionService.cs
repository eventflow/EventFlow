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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.EventStores;
using EventFlow.Extensions;

namespace EventFlow.Sagas
{
    public class SagaTypeDetails
    {
        private readonly ISet<Type> _startedBy;
        public Type SagaType { get; }
        public Type SagaLocatorType { get; }
        public Func<IEventStore, ISagaId, CancellationToken, Task<ISaga>> Loader { get; } 

        public SagaTypeDetails(
            Type sagaType,
            Type sagaLocatorType,
            IEnumerable<Type> startedBy)
        {
            _startedBy = new HashSet<Type>(startedBy);

            SagaType = sagaType;
            SagaLocatorType = sagaLocatorType;

            var methodInfo = sagaType.BaseType.GetMethods().Single(mi => mi.Name == "LoadSagaAsync");
            Loader = (es, i, c) => ((Task<ISaga>) methodInfo.Invoke(null, new object[] { es, i, c }));
        }

        public bool IsStartedBy(Type aggregateEventType) { return _startedBy.Contains(aggregateEventType); }
    }

    public class SagaDefinitionService : ISagaDefinitionService
    {
        private readonly ConcurrentDictionary<Type, List<SagaTypeDetails>> _sagaTypeDetailsByAggregateEvent = new ConcurrentDictionary<Type, List<SagaTypeDetails>>();

        public void LoadSagas(params Type[] sagaTypes)
        {
            LoadSagas((IEnumerable<Type>)sagaTypes);
        }

        public void LoadSagas(IEnumerable<Type> sagaTypes)
        {
            foreach (var sagaType in sagaTypes)
            {
                if (!typeof (ISaga).IsAssignableFrom(sagaType))
                {
                    throw new ArgumentException(
                        $"Type {sagaType.PrettyPrint()} is not a {typeof(ISaga).PrettyPrint()}",
                        nameof(sagaTypes));
                }

                var sagaInterfaces = sagaType.GetInterfaces();
                var sagaHandlesTypes = sagaInterfaces
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (ISagaHandles<,,>))
                    .ToList();
                var sagaStartedByTypes = sagaInterfaces
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (ISagaIsStartedBy<,,>))
                    .Select(i => i.GetGenericArguments()[2])
                    .ToList();
                var aggregateEventTypes = sagaHandlesTypes
                    .Select(i => i.GetGenericArguments()[2]);
                var sagaInterfaceType = sagaInterfaces.Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISaga<,>));

                var sagaTypeDetails = new SagaTypeDetails(
                    sagaType,
                    sagaInterfaceType.GetGenericArguments()[1],
                    sagaStartedByTypes);

                foreach (var aggregateEventType in aggregateEventTypes)
                {
                    _sagaTypeDetailsByAggregateEvent[aggregateEventType] = new List<SagaTypeDetails>(new [] { sagaTypeDetails });
                }
            }
        }

        public IEnumerable<SagaTypeDetails> GetSagaTypeDetails(Type aggregateEventType)
        {
            List<SagaTypeDetails> sagaTypeDetails;
            return _sagaTypeDetailsByAggregateEvent.TryGetValue(aggregateEventType, out sagaTypeDetails)
                ? sagaTypeDetails
                : Enumerable.Empty<SagaTypeDetails>();
        }
    }
}
