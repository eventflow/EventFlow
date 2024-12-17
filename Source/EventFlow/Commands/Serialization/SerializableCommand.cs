// The MIT License (MIT)
// 
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
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

using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Attributes;
using EventFlow.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static EventFlow.Core.GuidFactories.Deterministic;

namespace EventFlow.Commands.Serialization
{
    public abstract class SerializableCommand<TAggregate, TIdentity, TExecutionResult> : ICommand<TAggregate, TIdentity, TExecutionResult>
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : class, IIdentity
        where TExecutionResult : IExecutionResult
    {

        private readonly Lazy<ISourceId> lazySourceId;
        private readonly Lazy<TIdentity> lazyAggregateId;
        [JsonIgnore]
        public ISourceId SourceId => lazySourceId.Value;
        [JsonIgnore]
        public TIdentity AggregateId => lazyAggregateId.Value;
        
        [AggregateIdDisplayValue]
        public string AggregateValue { get; }
        protected SerializableCommand(string aggregateValue)
        {
            AggregateValue = aggregateValue;

            lazySourceId = new Lazy<ISourceId>(CalculateSourceId, LazyThreadSafetyMode.PublicationOnly);
            lazyAggregateId = new Lazy<TIdentity>(CreateAggregateId, LazyThreadSafetyMode.PublicationOnly);
        }

        private CommandId CalculateSourceId()
        {
            var bytes = GetSourceIdComponents().SelectMany(b => b).ToArray();
            return CommandId.NewDeterministic(
                GuidFactories.Deterministic.Namespaces.Commands,
                bytes);
        }

        private TIdentity CreateAggregateId()
        {
            if (AggregateValue == null) throw new ArgumentNullException(nameof(AggregateValue));
            return CreateAggregateId(AggregateValue);
        }

        protected abstract IEnumerable<byte[]> GetSourceIdComponents();

        public async Task<IExecutionResult> PublishAsync(ICommandBus commandBus, CancellationToken cancellationToken)
        {
            return await commandBus.PublishAsync(this, cancellationToken).ConfigureAwait(false);
        }

        protected static TIdentity CreateAggregateId(string aggregateValue)
        {
            var createdIdentity = Activator.CreateInstance(typeof(TIdentity), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, default,
                new[] { aggregateValue }, default) as TIdentity;

            if (createdIdentity == null)
            {
                throw new ArgumentNullException("Aggregate value provided is not in the correct format.");
            }

            return createdIdentity;
        }

        protected static string NewAggregateValue()
        {
            var identityName = new Regex("Id$").Replace(typeof(TIdentity).Name, string.Empty).ToLowerInvariant();
            return $"{identityName}-{GuidFactories.Comb.CreateForString()}";
        }

        protected static string NewDeterministic(string baseValue)
        {
            return GuidFactories.Deterministic.Create(Namespaces.Commands, baseValue).ToString();
        }

        public ISourceId GetSourceId()
        {
            return SourceId;
        }
    }

    public abstract class SerializableCommand<TAggregate, TIdentity> : SerializableCommand<TAggregate, TIdentity, IExecutionResult>
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : class, IIdentity
    {
        protected SerializableCommand(string aggregateValue) : base(aggregateValue)
        { }
    }
}