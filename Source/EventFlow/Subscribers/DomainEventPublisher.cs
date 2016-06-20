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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Logs;
using EventFlow.ReadStores;

namespace EventFlow.Subscribers
{
    public class DomainEventPublisher : IDomainEventPublisher
    {
        private readonly ILog _log;
        private readonly IEventFlowConfiguration _eventFlowConfiguration;
        private readonly IDispatchToEventSubscribers _dispatchToEventSubscribers;
        private readonly IReadOnlyCollection<ISubscribeSynchronousToAll> _subscribeSynchronousToAlls;
        private readonly IReadOnlyCollection<IReadStoreManager> _readStoreManagers;

        public DomainEventPublisher(
            ILog log,
            IEventFlowConfiguration eventFlowConfiguration,
            IDispatchToEventSubscribers dispatchToEventSubscribers,
            IEnumerable<IReadStoreManager> readStoreManagers,
            IEnumerable<ISubscribeSynchronousToAll> subscribeSynchronousToAlls)
        {
            _log = log;
            _eventFlowConfiguration = eventFlowConfiguration;
            _dispatchToEventSubscribers = dispatchToEventSubscribers;
            _subscribeSynchronousToAlls = subscribeSynchronousToAlls.ToList();
            _readStoreManagers = readStoreManagers.ToList();
        }

        public async Task PublishAsync<TAggregate, TIdentity>(
            TIdentity id,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            // ARGH, dilemma, should we pass the cancellation token to read model update or not?
            var updateReadStoresTasks = _readStoreManagers
                .Select(rsm => rsm.UpdateReadStoresAsync(domainEvents, CancellationToken.None));
            await Task.WhenAll(updateReadStoresTasks).ConfigureAwait(false);

            // Send to handlers that listen to all events
            var handle = _subscribeSynchronousToAlls
                .Select(s => s.HandleAsync(domainEvents, cancellationToken));
            await Task.WhenAll(handle).ConfigureAwait(false);

            try
            {
                // Update subscriptions AFTER read stores have been updated
                await _dispatchToEventSubscribers.DispatchAsync(domainEvents, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (!_eventFlowConfiguration.ThrowSubscriberExceptions)
            {
                _log.Debug(e, "Swallowed ");
            }
        }
    }
}