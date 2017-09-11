// The MIT License (MIT)
// 
// Copyright (c) 2015-2017 Rasmus Mikkelsen
// Copyright (c) 2015-2017 eBay Software Foundation
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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.Subscribers;

namespace EventFlow.Domain
{
    public class DomainDescriber : IDomainDescriber
    {
        private readonly IScopeResolver _resolver;
        private readonly IEventDefinitionService _eventDefinitionService;

        public DomainDescriber(
            IScopeResolver resolver,
            IEventDefinitionService eventDefinitionService)
        {
            _resolver = resolver;
            _eventDefinitionService = eventDefinitionService;
        }

        public Task<string> BuildGraphAsync(bool resolveSubscribers, CancellationToken cancellationToken)
        {
            var graph = string.Join(Environment.NewLine, GenerateGraph(resolveSubscribers));
            return Task.FromResult(graph);
        }

        private IEnumerable<string> GenerateGraph(bool resolveSubscribers)
        {
            yield return "digraph G {";
            yield return "   rankdir=LR;";

            foreach (var g in _eventDefinitionService
                .GetAllDefinitions()
                .GroupBy(d => d.AggregateType))
            {
                var aggregateName = g.Key.PrettyPrint();
                yield return $"   subgraph cluster_{aggregateName} {{";
                yield return $"      label = \"{aggregateName}\";";
                yield return $"      {aggregateName} [style=filled,fillcolor=lightgrey]";
                yield return "       node [shape=box,style=filled,fillcolor=white];";
                yield return "       color=grey;";

                foreach (var eventDefinition in g)
                {
                    yield return $"       {aggregateName} -> {GetNodeName(eventDefinition)}";
                }

                yield return "   }";
                yield return string.Empty;

                if (!resolveSubscribers)
                {
                    continue;
                }
                
                foreach (var subscriberDetails in g.SelectMany(GatherSubscriberDetails))
                {
                    yield return $"   {GetNodeName(subscriberDetails.EventDefinition)} -> {subscriberDetails.SubscriberType.PrettyPrint()}";
                }
                yield return string.Empty;
            }

            yield return "}";
        }

        private static string GetNodeName(EventDefinition eventDefinition)
        {
            return $"{eventDefinition.Name}V{eventDefinition.Version}";
        }

        private IEnumerable<SubscriberDetails> GatherSubscriberDetails(EventDefinition eventDefinition)
        {
            using (var scope = _resolver.BeginScope())
            {
                var subscriberServiceType = typeof(ISubscribeSynchronousTo<,,>).MakeGenericType(
                    eventDefinition.AggregateType,
                    eventDefinition.IdentityType,
                    eventDefinition.Type);
                
                foreach (var subscriber in scope.ResolveAll(subscriberServiceType))
                {
                    var subscriberImplementationType = subscriber.GetType();

                    yield return new SubscriberDetails(
                        subscriberImplementationType,
                        eventDefinition);
                }
            }
        }

        private class SubscriberDetails
        {
            public Type SubscriberType { get; }
            public EventDefinition EventDefinition { get; }

            public SubscriberDetails(
                Type subscriberType,
                EventDefinition eventDefinition)
            {
                SubscriberType = subscriberType;
                EventDefinition = eventDefinition;
            }
        }
    }
}