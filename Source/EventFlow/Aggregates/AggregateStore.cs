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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Configuration;
using EventFlow.Configuration.Cancellation;
using EventFlow.Core;
using EventFlow.Core.RetryStrategies;
using EventFlow.EventStores;
using EventFlow.Exceptions;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.Snapshots;
using EventFlow.Subscribers;

namespace EventFlow.Aggregates
{
    public class AggregateStore : IAggregateStore
    {
        private static readonly IReadOnlyCollection<IDomainEvent> EmptyDomainEventCollection = new IDomainEvent[] { };
        private readonly ILog _log;
        private readonly IResolver _resolver;
        private readonly IAggregateFactory _aggregateFactory;
        private readonly IEventStore _eventStore;
        private readonly ISnapshotStore _snapshotStore;
        private readonly ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> _transientFaultHandler;
        private readonly ICancellationConfiguration _cancellationConfiguration;
        private readonly IAggregateStoreResilienceStrategy _aggregateStoreResilienceStrategy;
        private readonly IEventFlowConfiguration _eventFlowConfiguration;

        public AggregateStore(
            ILog log,
            IResolver resolver,
            IAggregateFactory aggregateFactory,
            IEventStore eventStore,
            ISnapshotStore snapshotStore,
            ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> transientFaultHandler,
            ICancellationConfiguration cancellationConfiguration,
            IAggregateStoreResilienceStrategy aggregateStoreResilienceStrategy,
            IEventFlowConfiguration eventFlowConfiguration)
        {
            _log = log;
            _resolver = resolver;
            _aggregateFactory = aggregateFactory;
            _eventStore = eventStore;
            _snapshotStore = snapshotStore;
            _transientFaultHandler = transientFaultHandler;
            _cancellationConfiguration = cancellationConfiguration;
            _aggregateStoreResilienceStrategy = aggregateStoreResilienceStrategy;
            _eventFlowConfiguration = eventFlowConfiguration;
        }

        public async Task<TAggregate> LoadAsync<TAggregate, TIdentity>(
            TIdentity id,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            var aggregate = await _aggregateFactory.CreateNewAggregateAsync<TAggregate, TIdentity>(id).ConfigureAwait(false);
            await aggregate.LoadAsync(_eventStore, _snapshotStore, cancellationToken).ConfigureAwait(false);
            return aggregate;
        }

        public async Task<IReadOnlyCollection<IDomainEvent>> UpdateAsync<TAggregate, TIdentity>(
            TIdentity id,
            ISourceId sourceId,
            Func<TAggregate, CancellationToken, Task> updateAggregate,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            var aggregateUpdateResult = await UpdateAsync<TAggregate, TIdentity, IExecutionResult>(
                id,
                sourceId,
                async (a, c) =>
                    {
                        await updateAggregate(a, c).ConfigureAwait(false);
                        return ExecutionResult.Success();
                    },
                cancellationToken)
                .ConfigureAwait(false);

            return aggregateUpdateResult.DomainEvents;
        }

        public async Task<IAggregateUpdateResult<TExecutionResult>> UpdateAsync<TAggregate, TIdentity, TExecutionResult>(
            TIdentity id,
            ISourceId sourceId,
            Func<TAggregate, CancellationToken, Task<TExecutionResult>> updateAggregate,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            await _aggregateStoreResilienceStrategy.BeforeAggregateUpdate<TAggregate, TIdentity, TExecutionResult>(
                    id,
                    cancellationToken)
                .ConfigureAwait(false);

            var aggregateUpdateResult = await _transientFaultHandler.TryAsync(
                async c =>
                {
                    var aggregate = await LoadAsync<TAggregate, TIdentity>(id, c).ConfigureAwait(false);
                    if (aggregate.HasSourceId(sourceId))
                    {
                        throw new DuplicateOperationException(
                            sourceId,
                            id,
                            $"Aggregate '{typeof(TAggregate).PrettyPrint()}' has already had operation '{sourceId}' performed");
                    }

                    cancellationToken = _cancellationConfiguration.Limit(cancellationToken, CancellationBoundary.BeforeUpdatingAggregate);

                    var result = await updateAggregate(aggregate, c).ConfigureAwait(false);
                    if (!result.IsSuccess)
                    {
                        _log.Debug(() => $"Execution failed on aggregate '{typeof(TAggregate).PrettyPrint()}', disregarding any events emitted");
                        return new AggregateUpdateResult<TExecutionResult>(
                            result,
                            EmptyDomainEventCollection);
                    }

                    cancellationToken = _cancellationConfiguration.Limit(cancellationToken, CancellationBoundary.BeforeCommittingEvents);

                    await _aggregateStoreResilienceStrategy.BeforeCommitAsync<TAggregate, TIdentity, TExecutionResult>(
                            aggregate,
                            result,
                            cancellationToken)
                        .ConfigureAwait(false);
                    try
                    {
                        var domainEvents = await aggregate.CommitAsync(
                                _eventStore,
                                _snapshotStore,
                                sourceId,
                                cancellationToken)
                            .ConfigureAwait(false);
                        await _aggregateStoreResilienceStrategy.CommitSucceededAsync<TAggregate, TIdentity, TExecutionResult>(
                                aggregate,
                                result,
                                cancellationToken)
                            .ConfigureAwait(false);
                        return new AggregateUpdateResult<TExecutionResult>(
                            result,
                            domainEvents);
                    }
                    catch (OptimisticConcurrencyException) when (!_eventFlowConfiguration.ForwardOptimisticConcurrencyExceptions)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        var (handled, updatedAggregateUpdateResult) = await _aggregateStoreResilienceStrategy.HandleCommitFailedAsync<TAggregate, TIdentity, TExecutionResult>(
                                aggregate,
                                result,
                                e,
                                cancellationToken)
                            .ConfigureAwait(false);
                        if (!handled)
                        {
                            throw;
                        }

                        return updatedAggregateUpdateResult;
                    }
                },
                Label.Named("aggregate-update"),
                cancellationToken)
                .ConfigureAwait(false);

            if (aggregateUpdateResult.Result.IsSuccess &&
                aggregateUpdateResult.DomainEvents.Any())
            {
                await _aggregateStoreResilienceStrategy.BeforeEventPublishAsync<TAggregate, TIdentity, TExecutionResult>(
                        id,
                        aggregateUpdateResult.Result,
                        aggregateUpdateResult.DomainEvents,
                        cancellationToken)
                    .ConfigureAwait(false);
                try
                {
                    var domainEventPublisher = _resolver.Resolve<IDomainEventPublisher>();
                    await domainEventPublisher.PublishAsync(
                            aggregateUpdateResult.DomainEvents,
                            cancellationToken)
                        .ConfigureAwait(false);
                    await _aggregateStoreResilienceStrategy.EventPublishSucceededAsync<TAggregate, TIdentity, TExecutionResult>(
                            id,
                            aggregateUpdateResult.Result,
                            aggregateUpdateResult.DomainEvents,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    if (!await _aggregateStoreResilienceStrategy.HandleEventPublishFailedAsync<TAggregate, TIdentity, TExecutionResult>(
                            id,
                            aggregateUpdateResult.Result,
                            aggregateUpdateResult.DomainEvents,
                            e,
                            cancellationToken)
                        .ConfigureAwait(false))
                    {
                        throw;
                    }
                }
            }
            else
            {
                await _aggregateStoreResilienceStrategy.EventPublishSkippedAsync<TAggregate, TIdentity, TExecutionResult>(
                        id,
                        aggregateUpdateResult.Result,
                        aggregateUpdateResult.DomainEvents,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return aggregateUpdateResult;
        }

        public async Task<IReadOnlyCollection<IDomainEvent>> StoreAsync<TAggregate, TIdentity>(
            TAggregate aggregate,
            ISourceId sourceId,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            var domainEvents = await aggregate.CommitAsync(
                _eventStore,
                _snapshotStore,
                sourceId,
                cancellationToken)
                .ConfigureAwait(false);

            if (domainEvents.Any())
            {
                var domainEventPublisher = _resolver.Resolve<IDomainEventPublisher>();
                await domainEventPublisher.PublishAsync(
                    domainEvents,
                    cancellationToken)
                    .ConfigureAwait(false);
            }

            return domainEvents;
        }
        
        internal class AggregateUpdateResult<TExecutionResult> : IAggregateUpdateResult<TExecutionResult>
            where TExecutionResult : IExecutionResult
        {
            public TExecutionResult Result { get; }
            public IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

            public AggregateUpdateResult(
                TExecutionResult result,
                IReadOnlyCollection<IDomainEvent> domainEvents)
            {
                Result = result;
                DomainEvents = domainEvents;
            }
        }
    }
}
