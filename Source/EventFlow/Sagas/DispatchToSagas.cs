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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Extensions;
using EventFlow.Logs;

namespace EventFlow.Sagas
{
    public class DispatchToSagas : IDispatchToSagas
    {
        private readonly ILog _log;
        private readonly IResolver _resolver;
        private readonly ISagaStore _sagaStore;
        private readonly ISagaDefinitionService _sagaDefinitionService;
        private readonly ISagaErrorHandler _sagaErrorHandler;

        public DispatchToSagas(
            ILog log,
            IResolver resolver,
            ISagaStore sagaStore,
            ISagaDefinitionService sagaDefinitionService,
            ISagaErrorHandler sagaErrorHandler)
        {
            _log = log;
            _resolver = resolver;
            _sagaStore = sagaStore;
            _sagaDefinitionService = sagaDefinitionService;
            _sagaErrorHandler = sagaErrorHandler;
        }

        public async Task ProcessAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            foreach (var domainEvent in domainEvents)
            {
                await ProcessAsync(
                    domainEvent,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task ProcessAsync(
            IDomainEvent domainEvent,
            CancellationToken cancellationToken)
        {
            var sagaTypeDetails = _sagaDefinitionService.GetSagaDetails(domainEvent.EventType);

            _log.Verbose(() => $"Saga types to process for domain event '{domainEvent.EventType.PrettyPrint()}': {string.Join(", ", sagaTypeDetails.Select(d => d.SagaType.PrettyPrint()))}");

            foreach (var details in sagaTypeDetails)
            {
                var locator = (ISagaLocator) _resolver.Resolve(details.SagaLocatorType);
                var sagaId = await locator.LocateSagaAsync(domainEvent, cancellationToken).ConfigureAwait(false);

                if (sagaId == null)
                {
                    _log.Verbose(() => $"Saga locator '{details.SagaLocatorType.PrettyPrint()}' returned null");
                    continue;
                }

                await ProcessSagaAsync(domainEvent, sagaId, details, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ProcessSagaAsync(
            IDomainEvent domainEvent,
            ISagaId sagaId,
            SagaDetails details,
            CancellationToken cancellationToken)
        {
            try
            {
                _log.Verbose(() => $"Loading saga '{details.SagaType.PrettyPrint()}' with ID '{sagaId}'");

                await _sagaStore.UpdateAsync(
                    sagaId,
                    details.SagaType,
                    domainEvent.Metadata.EventId,
                    (s, c) => UpdateSagaAsync(s, domainEvent, details, c),
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var handled = await _sagaErrorHandler.HandleAsync(
                    sagaId,
                    details,
                    e,
                    cancellationToken)
                    .ConfigureAwait(false);
                if (handled)
                {
                    return;
                }

                _log.Error(e, $"Failed to process domain event '{domainEvent.EventType}' for saga '{details.SagaType.PrettyPrint()}'");
                throw;
            }
        }

        private Task UpdateSagaAsync(
            ISaga saga,
            IDomainEvent domainEvent,
            SagaDetails details,
            CancellationToken cancellationToken)
        {
            if (saga.State == SagaState.Completed)
            {
                _log.Debug(() => string.Format(
                    "Saga '{0}' is completed, skipping processing of '{1}'",
                    details.SagaType.PrettyPrint(),
                    domainEvent.EventType.PrettyPrint()));
                return Task.FromResult(0);
            }

            if (saga.State == SagaState.New && !details.IsStartedBy(domainEvent.EventType))
            {
                _log.Debug(() => string.Format(
                    "Saga '{0}' isn't started yet and not started by '{1}', skipping",
                    details.SagaType.PrettyPrint(),
                    domainEvent.EventType.PrettyPrint()));
                return Task.FromResult(0);
            }

            var sagaUpdaterType = typeof(ISagaUpdater<,,,>).MakeGenericType(
                domainEvent.AggregateType,
                domainEvent.IdentityType,
                domainEvent.EventType,
                details.SagaType);
            var sagaUpdater = (ISagaUpdater)_resolver.Resolve(sagaUpdaterType);

            return sagaUpdater.ProcessAsync(
                saga,
                domainEvent,
                SagaContext.Empty,
                cancellationToken);
        }
    }
}