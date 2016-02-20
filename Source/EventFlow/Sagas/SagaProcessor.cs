// The MIT License (MIT)
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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Extensions;

namespace EventFlow.Sagas
{
    public class SagaProcessor<TAggregate, TIdentity, TAggregateEvent, TSaga> : ISagaProcessor<TAggregate, TIdentity, TAggregateEvent, TSaga>
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
        where TAggregateEvent : IAggregateEvent<TAggregate, TIdentity>
        where TSaga : ISaga
    {
        private readonly ICommandBus _commandBus;
        private readonly IEventStore _eventStore;

        public SagaProcessor(
            ICommandBus commandBus,
            IEventStore eventStore)
        {
            _commandBus = commandBus;
            _eventStore = eventStore;
        }

        public async Task ProcessAsync(ISaga saga, IDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            var specificDomainEvent = domainEvent as IDomainEvent<TAggregate, TIdentity, TAggregateEvent>;
            var specificSaga = saga as ISagaHandles<TAggregate, TIdentity, TAggregateEvent>;

            if (specificDomainEvent == null) throw new ArgumentException($"Domain event is not of type '{typeof(IDomainEvent<TAggregate, TIdentity, TAggregateEvent>).PrettyPrint()}'");
            if (specificSaga == null) throw new ArgumentException($"Saga is not of type '{typeof(ISagaHandles<TAggregate, TIdentity, TAggregateEvent>).PrettyPrint()}'");

            await specificSaga.ProcessAsync(specificDomainEvent, cancellationToken).ConfigureAwait(false);
            await saga.CommitAsync(_eventStore, domainEvent.Metadata.EventId, cancellationToken).ConfigureAwait(false);
            await saga.PublishAsync(_commandBus, cancellationToken).ConfigureAwait(false);
        }
    }
}