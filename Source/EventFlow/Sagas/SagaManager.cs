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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.EventStores;
using EventFlow.Logs;

namespace EventFlow.Sagas
{
    public class SagaManager : ISagaManager
    {
        private readonly ILog _log;
        private readonly IResolver _resolver;
        private readonly IEventStore _eventStore;
        private readonly ISagaDefinitionService _sagaDefinitionService;

        public SagaManager(
            ILog log,
            IResolver resolver,
            IEventStore eventStore,
            ISagaDefinitionService sagaDefinitionService)
        {
            _log = log;
            _resolver = resolver;
            _eventStore = eventStore;
            _sagaDefinitionService = sagaDefinitionService;
        }

        public Task ProcessAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            return Task.WhenAll(domainEvents.Select(d => ProcessAsync(d, cancellationToken)));
        }

        private async Task ProcessAsync(
            IDomainEvent domainEvent,
            CancellationToken cancellationToken)
        {
            var sagaTypeDetails = _sagaDefinitionService.GetSagaTypeDetails(domainEvent.EventType);

            foreach (var details in sagaTypeDetails)
            {
                var locator = (ISagaLocator) _resolver.Resolve(details.SagaLocatorType);
                var sagaId = await locator.LocateSagaAsync(domainEvent, cancellationToken).ConfigureAwait(false);
                var saga = await details.Loader(_eventStore, sagaId, cancellationToken).ConfigureAwait(false);
                await domainEvent.InvokeSagaAsync(saga, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
