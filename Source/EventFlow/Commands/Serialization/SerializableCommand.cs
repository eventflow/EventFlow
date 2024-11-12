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
using EventFlow.Core;
using System;
using System.Reflection;
using System.Text.Json.Serialization;

namespace EventFlow.Commands.Serialization
{
    public abstract class SerializableCommand<TAggregate, TIdentity, TExecutionResult> : DistinctCommand<TAggregate, TIdentity, TExecutionResult>
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : class, IIdentity
        where TExecutionResult : IExecutionResult
    {
        [JsonIgnore]
        public new ISourceId SourceId => base.SourceId;
        [JsonIgnore]
        public new TIdentity AggregateId => base.AggregateId;
        public abstract string AggregateValue { get; protected set; }
        protected SerializableCommand(string aggregateValue) : base(CreateAggregateId(aggregateValue))
        {}

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
    }

    public abstract class SerializableCommand<TAggregate, TIdentity> : SerializableCommand<TAggregate, TIdentity, IExecutionResult>
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : class, IIdentity
    {
        protected SerializableCommand(string aggregateValue) : base(aggregateValue)
        { }
    }
}