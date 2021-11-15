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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventFlow.Sagas
{
    public class DispatchToSagas : IDispatchToSagas
    {
        private readonly ILogger<DispatchToSagas> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISagaStore _sagaStore;
        private readonly ISagaDefinitionService _sagaDefinitionService;
        private readonly ISagaErrorHandler _sagaErrorHandler;
        private readonly ISagaUpdateResilienceStrategy _sagaUpdateLog;
        private readonly Func<Type, ISagaErrorHandler> _sagaErrorHandlerFactory;

        public DispatchToSagas(
            ILogger<DispatchToSagas> logger,
            IServiceProvider serviceProvider,
            ISagaStore sagaStore,
            ISagaDefinitionService sagaDefinitionService,
            ISagaErrorHandler sagaErrorHandler,
            ISagaUpdateResilienceStrategy sagaUpdateLog,
            Func<Type, ISagaErrorHandler> sagaErrorHandlerFactory)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _sagaStore = sagaStore;
            _sagaDefinitionService = sagaDefinitionService;
            _sagaErrorHandler = sagaErrorHandler;
            _sagaUpdateLog = sagaUpdateLog;
            _sagaErrorHandlerFactory = sagaErrorHandlerFactory;
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

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace(
                    "Saga types to process for domain event {DomainEventType}: {SagaTypes}",
                    domainEvent.EventType.PrettyPrint(),
                    sagaTypeDetails.Select(d => d.SagaType.PrettyPrint()));
            }

            foreach (var details in sagaTypeDetails)
            {
                var locator = (ISagaLocator) _serviceProvider.GetRequiredService(details.SagaLocatorType);
                var sagaId = await locator.LocateSagaAsync(domainEvent, cancellationToken).ConfigureAwait(false);

                if (sagaId == null)
                {
                    _logger.LogTrace(
                        "Saga locator {SagaLocatorType} returned null",
                        details.SagaLocatorType.PrettyPrint());
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
                _logger.LogTrace(
                    "Loading saga {SagaType} with ID {Id}",
                    details.SagaType.PrettyPrint(),
                    sagaId);

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
                // Search for a specific SagaErrorHandler<Saga> based on saga type
                ISagaErrorHandler specificSagaErrorHandler = _sagaErrorHandlerFactory(details.SagaType);

                bool handled = specificSagaErrorHandler != null ? 
                    await specificSagaErrorHandler.HandleAsync(sagaId, details, e, cancellationToken).ConfigureAwait(false) :
                    await _sagaErrorHandler.HandleAsync(sagaId, details, e, cancellationToken).ConfigureAwait(false);

                if (handled)
                {
                    return;
                }

                _logger.LogError(
                    "Failed to process domain event {DomainEventType} for saga {SagaType}",
                    domainEvent.EventType,
                    details.SagaType.PrettyPrint());
                throw;
            }
        }

        private async Task UpdateSagaAsync(
            ISaga saga,
            IDomainEvent domainEvent,
            SagaDetails details,
            CancellationToken cancellationToken)
        {
            if (saga.State == SagaState.Completed)
            {
                _logger.LogTrace(
                    "Saga {SagaType} is completed, skipping processing of {DomainEventType}",
                    details.SagaType.PrettyPrint(),
                    domainEvent.EventType.PrettyPrint());
                return;
            }

            if (saga.State == SagaState.New && !details.IsStartedBy(domainEvent.EventType))
            {
                _logger.LogTrace(
                    "Saga {SagaType} isn't started yet and not started by {DomainEventType}, skipping",
                    details.SagaType.PrettyPrint(),
                    domainEvent.EventType.PrettyPrint());
                return;
            }

            var sagaUpdaterType = typeof(ISagaUpdater<,,,>).MakeGenericType(
                domainEvent.AggregateType,
                domainEvent.IdentityType,
                domainEvent.EventType,
                details.SagaType);
            var sagaUpdater = (ISagaUpdater)_serviceProvider.GetRequiredService(sagaUpdaterType);

            await _sagaUpdateLog.BeforeUpdateAsync(
                    saga,
                    domainEvent,
                    details,
                    cancellationToken)
                .ConfigureAwait(false);
            try
            {
                await sagaUpdater.ProcessAsync(
                        saga,
                        domainEvent,
                        SagaContext.Empty,
                        cancellationToken)
                    .ConfigureAwait(false);
                await _sagaUpdateLog.UpdateSucceededAsync(
                        saga,
                        domainEvent,
                        details,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (!await _sagaUpdateLog.HandleUpdateFailedAsync(
                        saga,
                        domainEvent,
                        details,
                        e,
                        cancellationToken)
                    .ConfigureAwait(false))
                {
                    throw;
                }
            }
        }
    }
}
