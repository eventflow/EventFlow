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
using EventFlow.Core.RetryStrategies;
using EventFlow.EventStores;
using EventFlow.Exceptions;
using EventFlow.Logs;
using EventFlow.Subscribers;

namespace EventFlow
{
    public class CommandBus : ICommandBus
    {
        private readonly ILog _log;
        private readonly IResolver _resolver;
        private readonly IEventStore _eventStore;
        private readonly IDomainEventPublisher _domainEventPublisher;
        private readonly ITransientFaultHandler _transientFaultHandler;

        public CommandBus(
            ILog log,
            IResolver resolver,
            IEventStore eventStore,
            IDomainEventPublisher domainEventPublisher,
            ITransientFaultHandler transientFaultHandler)
        {
            _log = log;
            _resolver = resolver;
            _eventStore = eventStore;
            _domainEventPublisher = domainEventPublisher;
            _transientFaultHandler = transientFaultHandler;

            _transientFaultHandler.Use<IOptimisticConcurrencyRetryStrategy>();
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
                "Executing command '{0}' on aggregate '{1}'",
                commandTypeName,
                aggregateType);

            IReadOnlyCollection<IDomainEvent> domainEvents;
            try
            {
                domainEvents = await ExecuteCommandAsync(command, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _log.Debug(
                    exception,
                    "Excution of command '{0}' on aggregate '{1}' failed due to exception '{2}' with message: {3}",
                    commandTypeName,
                    aggregateType,
                    exception.GetType().Name,
                    exception.Message);
                throw;
            }

            if (!domainEvents.Any())
            {
                _log.Verbose(
                    "Execution command '{0}' on aggregate '{1}' did NOT result in any domain events",
                    commandTypeName,
                    aggregateType);
                return;
            }

            _log.Verbose(() => string.Format(
                "Execution command '{0}' on aggregate '{1}' resulted in these events: {2}",
                commandTypeName,
                aggregateType,
                string.Join(", ", domainEvents.Select(d => d.EventType.Name))));

            await _domainEventPublisher.PublishAsync<TAggregate>(command.Id, domainEvents, cancellationToken).ConfigureAwait(false);
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
            var aggregateType = typeof (TAggregate);
            var commandType = command.GetType();
            var commandHandlerType = typeof (ICommandHandler<,>).MakeGenericType(aggregateType, commandType);

            var commandHandlers = _resolver.ResolveAll(commandHandlerType).ToList();
            if (!commandHandlers.Any())
            {
                throw new NoCommandHandlersException(string.Format(
                    "No command handlers registered for the command '{0}' on aggregate '{1}'",
                    commandType.Name,
                    aggregateType.Name));
            }
            if (commandHandlers.Count > 1)
            {
                throw new InvalidOperationException(string.Format(
                    "Too many command handlers the command '{0}' on aggregate '{1}'. These were found: {2}",
                    commandType.Name,
                    aggregateType.Name,
                    string.Join(", ", commandHandlers.Select(h => h.GetType().Name))));
            }
            var commandHandler = commandHandlers.Single();

            var commandInvoker = commandHandlerType.GetMethod("ExecuteAsync");

            return _transientFaultHandler.TryAsync(
                async c =>
                    {
                        var aggregate = await _eventStore.LoadAggregateAsync<TAggregate>(command.Id, c).ConfigureAwait(false);

                        var invokeTask = (Task) commandInvoker.Invoke(commandHandler, new object[] {aggregate, command, c});
                        await invokeTask.ConfigureAwait(false);

                        return await aggregate.CommitAsync(_eventStore, c).ConfigureAwait(false);
                    },
                cancellationToken);
        }
    }
}
