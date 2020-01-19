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
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Extensions;
using EventFlow.Jobs;
using EventFlow.Sagas;
using EventFlow.Snapshots;
using Microsoft.Extensions.DependencyInjection;

namespace EventFlow
{
    internal class EventFlowBuilder : IEventFlowBuilder
    {
        private readonly List<Type> _aggregateEventTypes = new List<Type>();
        private readonly List<Type> _sagaTypes = new List<Type>(); 
        private readonly List<Type> _commandTypes = new List<Type>();
        private readonly List<Type> _jobTypes = new List<Type>();
        private readonly List<Type> _snapshotTypes = new List<Type>();

        public IServiceCollection Services { get; }

        public EventFlowBuilder(
            IServiceCollection serviceCollection)
        {
            Services = serviceCollection;
        }

        public IEventFlowBuilder AddEvents(IEnumerable<Type> aggregateEventTypes)
        {
            foreach (var aggregateEventType in aggregateEventTypes)
            {
                if (!typeof(IAggregateEvent).GetTypeInfo().IsAssignableFrom(aggregateEventType))
                {
                    throw new ArgumentException($"Type {aggregateEventType.PrettyPrint()} is not a {typeof(IAggregateEvent).PrettyPrint()}");
                }

                _aggregateEventTypes.Add(aggregateEventType);
            }

            return this;
        }

        public IEventFlowBuilder AddSagas(IEnumerable<Type> sagaTypes)
        {
            foreach (var sagaType in sagaTypes)
            {
                if (!typeof(ISaga).GetTypeInfo().IsAssignableFrom(sagaType))
                {
                    throw new ArgumentException($"Type {sagaType.PrettyPrint()} is not a {typeof(ISaga).PrettyPrint()}");
                }

                _sagaTypes.Add(sagaType);
            }

            return this;
        }

        public IEventFlowBuilder AddCommands(IEnumerable<Type> commandTypes)
        {
            foreach (var commandType in commandTypes)
            {
                if (!typeof(ICommand).GetTypeInfo().IsAssignableFrom(commandType))
                {
                    throw new ArgumentException($"Type {commandType.PrettyPrint()} is not a {typeof(ICommand).PrettyPrint()}");
                }

                _commandTypes.Add(commandType);
            }

            return this;
        }

        public IEventFlowBuilder AddJobs(IEnumerable<Type> jobTypes)
        {
            foreach (var jobType in jobTypes)
            {
                if (!typeof(IJob).GetTypeInfo().IsAssignableFrom(jobType))
                {
                    throw new ArgumentException($"Type {jobType.PrettyPrint()} is not a {typeof(IJob).PrettyPrint()}");
                }

                _jobTypes.Add(jobType);
            }

            return this;
        }

        public IEventFlowBuilder AddSnapshots(IEnumerable<Type> snapshotTypes)
        {
            foreach (var snapshotType in snapshotTypes)
            {
                if (!typeof(ISnapshot).GetTypeInfo().IsAssignableFrom(snapshotType))
                {
                    throw new ArgumentException($"Type {snapshotType.PrettyPrint()} is not a {typeof(ISnapshot).PrettyPrint()}");
                }
                _snapshotTypes.Add(snapshotType);
            }

            return this;
        }
    }
}
