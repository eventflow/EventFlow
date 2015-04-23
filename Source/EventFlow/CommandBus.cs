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
using EventFlow.Commands;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Exceptions;
using EventFlow.Logs;
using EventFlow.Subscribers;

namespace EventFlow
{
    public class CommandBus : ICommandBus
    {
        private readonly ILog _log;
        private readonly IEventFlowConfiguration _configuration;
        private readonly IEventStore _eventStore;
        private readonly IDomainEventPublisher _domainEventPublisher;

        public CommandBus(
            ILog log,
            IEventFlowConfiguration configuration,
            IEventStore eventStore,
            IDomainEventPublisher domainEventPublisher)
        {
            _log = log;
            _configuration = configuration;
            _eventStore = eventStore;
            _domainEventPublisher = domainEventPublisher;
        }

        public async Task PublishAsync<TAggregate>(
            ICommand<TAggregate> command,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot
        {
            if (command == null) throw new ArgumentNullException("command");

            var commandTypeName = command.GetType().Name;
            var aggregateType = typeof (TAggregate);
            _log.Verbose(
                "Executing command '{0}' on aggregate '{1}' with ID '{2}'",
                commandTypeName,
                aggregateType.Name,
                command.Id);

            IReadOnlyCollection<IDomainEvent> domainEvents;
            try
            {
                domainEvents = await ExecuteCommandAsync(command, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _log.Debug(
                    exception,
                    "Excution of command '{0}' on aggregate '{1}' with ID '{2}' failed due to exception '{3}' with message: {4}",
                    commandTypeName,
                    aggregateType.Name,
                    command.Id,
                    exception.GetType().Name,
                    exception.Message);
                throw;
            }

            if (!domainEvents.Any())
            {
                _log.Verbose(
                    "Execution command '{0}' on aggregate '{1}' with ID '{2}' did NOT result in any domain events",
                    commandTypeName,
                    aggregateType.Name,
                    command.Id);
                return;
            }

            // TODO: Determine if we can use cancellation token after there, this should be the "point of no return"
            await _domainEventPublisher.PublishAsync<TAggregate>(command.Id, domainEvents, CancellationToken.None).ConfigureAwait(false);
        }

        public void Publish<TAggregate>(ICommand<TAggregate> command) where TAggregate : IAggregateRoot
        {
            using (var a = AsyncHelper.Wait)
            {
                a.Run(PublishAsync(command, CancellationToken.None));
            }
        }

        private Task<IReadOnlyCollection<IDomainEvent>> ExecuteCommandAsync<TAggregate>(
            ICommand<TAggregate> command,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot
        {
            return Retry.ThisAsync(
                async () =>
                    {
                        var aggregate = await _eventStore.LoadAggregateAsync<TAggregate>(command.Id, cancellationToken).ConfigureAwait(false);
                        await command.ExecuteAsync(aggregate, cancellationToken).ConfigureAwait(false);
                        return await aggregate.CommitAsync(_eventStore, cancellationToken).ConfigureAwait(false);
                    },
                _configuration.NumberOfRetriesOnOptimisticConcurrencyExceptions,
                new[] { typeof(OptimisticConcurrencyException) },
                _configuration.DelayBeforeRetryOnOptimisticConcurrencyExceptions);
        }
    }
}
