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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;

namespace EventFlow.Bdd.Contexts
{
    public class GivenContext : IGivenContext
    {
        private readonly IEventStore _eventStore;

        public GivenContext(
            IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public IGivenContext Event<T>(IIdentity identity) where T : IAggregateEvent
        {
            throw new NotImplementedException();
        }

        public IGivenContext Event<T>(IIdentity identity, T aggregateEvent) where T : IAggregateEvent
        {
            InjectEvents(identity, aggregateEvent);
            return this;
        }

        protected IReadOnlyCollection<IDomainEvent> InjectEvents(IIdentity identity, params IAggregateEvent[] aggregateEvents)
        {
            IReadOnlyCollection<IDomainEvent> domainEvents = null;
            using (var a = AsyncHelper.Wait)
            {
                a.Run(InjectEventsAsync(identity, aggregateEvents), r => domainEvents = r);
            }
            return domainEvents;
        }

        protected async Task<IReadOnlyCollection<IDomainEvent>> InjectEventsAsync(
            IIdentity identity,
            params IAggregateEvent[] aggregateEvents)
        {
            var domainEvents = new List<IDomainEvent>();

            foreach (var a in aggregateEvents.Select((e, i) => new {Index = i, AggregateEvent = e}))
            {
                var metadata = new Metadata
                    {
                        SourceId = SourceId.New,
                        Timestamp = DateTimeOffset.Now,
                        AggregateSequenceNumber = a.Index // TODO: Find a better way
                    };

                var domainEvent = await a.AggregateEvent.StoreAsync(
                    _eventStore,
                    identity,
                    metadata,
                    CancellationToken.None)
                    .ConfigureAwait(false);

                domainEvents.Add(domainEvent);
            }

            return domainEvents;
        }
    }
}