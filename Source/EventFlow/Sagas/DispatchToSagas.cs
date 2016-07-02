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
                await ProcessAsync(domainEvent, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ProcessAsync(
            IDomainEvent domainEvent,
            CancellationToken cancellationToken)
        {
            var sagaTypeDetails = _sagaDefinitionService.GetSagaDetails(domainEvent.EventType);
            var commandBus = _resolver.Resolve<ICommandBus>();

            foreach (var details in sagaTypeDetails)
            {
                _log.Verbose(() => $"Executing saga '{details.SagaType.PrettyPrint()}'");

                var locator = (ISagaLocator) _resolver.Resolve(details.SagaLocatorType);
                var sagaId = await locator.LocateSagaAsync(domainEvent, cancellationToken).ConfigureAwait(false);

                try
                {
                    var saga = await _sagaStore.LoadAsync(
                        sagaId,
                        details,
                        cancellationToken)
                        .ConfigureAwait(false);

                    if (saga.IsNew && !details.IsStartedBy(domainEvent.EventType))
                    {
                        _log.Debug(() => $"Saga '{details.SagaType.PrettyPrint()}' isn't started yet, skipping processing of '{domainEvent.EventType.PrettyPrint()}'");
                        continue;
                    }

                    var sagaProcessorType = typeof (ISagaProcessor<,,,>).MakeGenericType(
                        domainEvent.AggregateType,
                        domainEvent.IdentityType,
                        domainEvent.EventType,
                        details.SagaType);
                    var sagaProcessor = (ISagaProcessor) _resolver.Resolve(sagaProcessorType);

                    await sagaProcessor.ProcessAsync(
                        saga,
                        domainEvent,
                        cancellationToken)
                        .ConfigureAwait(false);

                    await _sagaStore.StoreAsync(
                        saga,
                        domainEvent.Metadata.EventId,
                        cancellationToken)
                        .ConfigureAwait(false);

                    await saga.PublishAsync(commandBus, cancellationToken).ConfigureAwait(false);
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
                        continue;
                    }

                    _log.Error(e, $"Failed to process domain event '{domainEvent.EventType}' for saga '{details.SagaType.PrettyPrint()}'");
                    throw;
                }
            }
        }
    }
}