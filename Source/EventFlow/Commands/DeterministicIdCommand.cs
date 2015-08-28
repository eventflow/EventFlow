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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using EventFlow.Aggregates;
using EventFlow.Core;

namespace EventFlow.Commands
{
    public abstract class DeterministicIdCommand<TAggregate, TIdentity> : ICommand<TAggregate, TIdentity>
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
    {
        private readonly Lazy<ISourceId> _lazySourceId;

        public ISourceId SourceId => _lazySourceId.Value;
        public TIdentity AggregateId { get; }

        protected DeterministicIdCommand(
            TIdentity aggregateId)
        {
            if (aggregateId == null) throw new ArgumentNullException(nameof(aggregateId));

            _lazySourceId = new Lazy<ISourceId>(CalculateSourceId, LazyThreadSafetyMode.PublicationOnly);

            AggregateId = aggregateId;
        }

        private ISourceId CalculateSourceId()
        {
            using (var memoryStream = new MemoryStream())
            {
                foreach (var equalityComponent in GetEqualityComponents())
                {
                    memoryStream.Write(equalityComponent, 0, equalityComponent.Length);
                }

                return CommandId.NewDeterministic(
                    GuidFactories.Deterministic.Namespaces.Commands,
                    memoryStream.ToArray());
            }
        }

        protected abstract IEnumerable<byte[]> GetEqualityComponents();
    }
}
