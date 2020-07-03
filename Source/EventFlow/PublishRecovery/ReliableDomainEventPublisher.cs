// The MIT License (MIT)
//
// Copyright (c) 2015-2018 Rasmus Mikkelsen
// Copyright (c) 2015-2018 eBay Software Foundation
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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Subscribers;

namespace EventFlow.PublishRecovery
{
    public sealed class ReliableDomainEventPublisher : IDomainEventPublisher
    {
        private readonly IDomainEventPublisher _nonReliableDomainEventPublisher;
        private readonly IReliableMarkProcessor _reliableMarkProcessor;

        public ReliableDomainEventPublisher(IDomainEventPublisher nonReliableDomainEventPublisher, IReliableMarkProcessor reliableMarkProcessor)
        {
            _nonReliableDomainEventPublisher = nonReliableDomainEventPublisher;
            _reliableMarkProcessor = reliableMarkProcessor;
        }

        public async Task PublishAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
        {
            await _nonReliableDomainEventPublisher.PublishAsync(domainEvents, cancellationToken).ConfigureAwait(false);

            await _reliableMarkProcessor.MarkEventsPublishedAsync(domainEvents);
        }

        [Obsolete("Use PublishAsync (without generics and aggregate identity)")]
        public async Task PublishAsync<TAggregate, TIdentity>(TIdentity id, IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken) where TAggregate : IAggregateRoot<TIdentity> where TIdentity : IIdentity
        {
            await _nonReliableDomainEventPublisher.PublishAsync<TAggregate, TIdentity>(id, domainEvents, cancellationToken);

            await _reliableMarkProcessor.MarkEventsPublishedAsync(domainEvents);
        }
    }
}