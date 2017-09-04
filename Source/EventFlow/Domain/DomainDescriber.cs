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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.EventStores;
using EventFlow.Extensions;

namespace EventFlow.Domain
{
    public class DomainDescriber : IDomainDescriber
    {
        private readonly IEventDefinitionService _eventDefinitionService;

        public DomainDescriber(
            IEventDefinitionService eventDefinitionService)
        {
            _eventDefinitionService = eventDefinitionService;
        }

        public Task<string> BuildGraphAsync(CancellationToken cancellationToken)
        {
            var graph = string.Join(Environment.NewLine, GenerateGraph());
            return Task.FromResult(graph);
        }

        private IEnumerable<string> GenerateGraph()
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
                    yield return $"       {aggregateName} -> {eventDefinition.Name}V{eventDefinition.Version}";
                }

                yield return "   }";
            }

            yield return "}";
        }
    }
}