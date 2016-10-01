﻿// The MIT License (MIT)
//
// Copyright (c) 2015-2016 Rasmus Mikkelsen
// Copyright (c) 2015-2016 eBay Software Foundation
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
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace EventFlow.Sagas
{
    public class SagaDefinitionService : ISagaDefinitionService
    {
        private readonly ConcurrentDictionary<Type, List<SagaDetails>> _sagaDetailsByAggregateEvent = new ConcurrentDictionary<Type, List<SagaDetails>>();

        public void LoadSagas(params Type[] sagaTypes)
        {
            LoadSagas((IEnumerable<Type>)sagaTypes);
        }

        public void LoadSagas(IEnumerable<Type> sagaTypes)
        {
            foreach (var sagaType in sagaTypes)
            {
                var sagaDetails = SagaDetails.From(sagaType);

                foreach (var aggregateEventType in sagaDetails.AggregateEventTypes)
                {
                    var sagaDetailsList = _sagaDetailsByAggregateEvent.GetOrAdd(
                        aggregateEventType,
                        new List<SagaDetails>());

                    sagaDetailsList.Add(sagaDetails);
                }
            }
        }

        public IEnumerable<SagaDetails> GetSagaDetails(Type aggregateEventType)
        {
            List<SagaDetails> sagaDetails;
            return _sagaDetailsByAggregateEvent.TryGetValue(aggregateEventType, out sagaDetails)
                ? sagaDetails
                : Enumerable.Empty<SagaDetails>();
        }
    }
}
