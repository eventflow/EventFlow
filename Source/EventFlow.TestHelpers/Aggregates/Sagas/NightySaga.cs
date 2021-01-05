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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Exceptions;
using EventFlow.Sagas;
using EventFlow.Sagas.AggregateSagas;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.Sagas.Events;
using EventFlow.TestHelpers.Aggregates.ValueObjects;

namespace EventFlow.TestHelpers.Aggregates.Sagas
{
    public class NightySaga : AggregateSaga<NightySaga, NightySagaId, NightySagaLocator>,
        ISagaIsStartedBy<NightyAggregate, NightyId, NightySagaStartRequestedEvent>,
        ISagaHandles<NightyAggregate, NightyId, NightyPingEvent>,
        ISagaHandles<NightyAggregate, NightyId, NightySagaCompleteRequestedEvent>,
        IEmit<NightySagaStartedEvent>,
        IEmit<NightySagaPingReceivedEvent>,
        IEmit<NightySagaCompletedEvent>
    {
        public IReadOnlyCollection<PingId> PingIdsSinceStarted => _pingIdsSinceStarted;
        private readonly List<PingId> _pingIdsSinceStarted = new List<PingId>();
        private NightyId _NightyId;

        public NightySaga(
            NightySagaId id)
            : base(id)
        {
        }

        public Task HandleAsync(
            IDomainEvent<NightyAggregate, NightyId, NightySagaStartRequestedEvent> domainEvent,
            ISagaContext sagaContext,
            CancellationToken cancellationToken)
        {
            // This check is redundant! We do it to verify EventFlow works correctly
            if (State != SagaState.New) throw DomainError.With("Saga must be new!");

            Emit(new NightySagaStartedEvent(domainEvent.AggregateIdentity));
            return Task.FromResult(0);
        }

        public Task HandleAsync(
            IDomainEvent<NightyAggregate, NightyId, NightyPingEvent> domainEvent,
            ISagaContext sagaContext,
            CancellationToken cancellationToken)
        {
            // This check is redundant! We do it to verify EventFlow works correctly
            if (State != SagaState.Running) throw DomainError.With("Saga must be running!");

            var pingId = domainEvent.AggregateEvent.PingId;

            Emit(new NightySagaPingReceivedEvent(pingId));
            Publish(new NightyAddMessageCommand(_NightyId, new NightyMessage(NightyMessageId.New, pingId.Value)));

            return Task.FromResult(0);
        }

        public Task HandleAsync(
            IDomainEvent<NightyAggregate, NightyId, NightySagaCompleteRequestedEvent> domainEvent,
            ISagaContext sagaContext,
            CancellationToken cancellationToken)
        {
            // This check is redundant! We do it to verify EventFlow works correctly
            if (State != SagaState.Running) throw DomainError.With("Saga must be running!");

            Emit(new NightySagaCompletedEvent());

            return Task.FromResult(0);
        }

        public void Apply(NightySagaStartedEvent aggregateEvent)
        {
            _NightyId = aggregateEvent.NightyId;
        }

        public void Apply(NightySagaPingReceivedEvent aggregateEvent)
        {
            _pingIdsSinceStarted.Add(aggregateEvent.PingId);
        }

        public void Apply(NightySagaCompletedEvent aggregateEvent)
        {
            Complete();
        }
    }
}
