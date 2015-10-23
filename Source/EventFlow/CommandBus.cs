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
using System.Collections.Concurrent;
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
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.Subscribers;

namespace EventFlow
{
    public class CommandBus : ICommandBus
    {
        private static readonly ConcurrentDictionary<Type, CommandExecutionDetails> CommandExecutionDetailsMap = new ConcurrentDictionary<Type, CommandExecutionDetails>();

        private readonly ILog _log;
        private readonly IResolver _resolver;
        private readonly IEventStore _eventStore;
        private readonly IDomainEventPublisher _domainEventPublisher;
        private readonly ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> _transientFaultHandler;

        public CommandBus(
            ILog log,
            IResolver resolver,
            IEventStore eventStore,
            IDomainEventPublisher domainEventPublisher,
            ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> transientFaultHandler)
        {
            _log = log;
            _resolver = resolver;
            _eventStore = eventStore;
            _domainEventPublisher = domainEventPublisher;
            _transientFaultHandler = transientFaultHandler;
        }

        public async Task<ISourceId> PublishAsync<TAggregate, TIdentity, TSourceIdentity>(
            ICommand<TAggregate, TIdentity, TSourceIdentity> command,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TSourceIdentity : ISourceId
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            _log.Verbose(() => $"Executing command '{command.GetType().PrettyPrint()}' with ID '{command.SourceId}' on aggregate '{typeof(TAggregate).PrettyPrint()}'");

            IReadOnlyCollection<IDomainEvent> domainEvents;
            try
            {
                domainEvents = await ExecuteCommandAsync(command, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _log.Debug(
                    exception,
                    "Excution of command '{0}' with ID '{1}' on aggregate '{2}' failed due to exception '{3}' with message: {4}",
                    command.GetType().PrettyPrint(),
                    command.SourceId,
                    typeof(TAggregate),
                    exception.GetType().PrettyPrint(),
                    exception.Message);
                throw;
            }

            if (!domainEvents.Any())
            {
                _log.Verbose(() => string.Format(
                    "Execution command '{0}' with ID '{1}' on aggregate '{2}' did NOT result in any domain events",
                    command.GetType().PrettyPrint(),
                    command.SourceId,
                    typeof(TAggregate).PrettyPrint()));
                return command.SourceId;
            }

            _log.Verbose(() => string.Format(
                "Execution command '{0}' with ID '{1}' on aggregate '{2}' resulted in these events: {3}",
                command.GetType().PrettyPrint(),
                command.SourceId,
                typeof(TAggregate),
                string.Join(", ", domainEvents.Select(d => d.EventType.PrettyPrint()))));

            await _domainEventPublisher.PublishAsync<TAggregate, TIdentity>(
                command.AggregateId,
                domainEvents,
                cancellationToken)
                .ConfigureAwait(false);

            return command.SourceId;
        }

        public ISourceId Publish<TAggregate, TIdentity, TSourceIdentity>(
            ICommand<TAggregate, TIdentity, TSourceIdentity> command,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TSourceIdentity : ISourceId
        {
            ISourceId sourceId = null;

            using (var a = AsyncHelper.Wait)
            {
                a.Run(PublishAsync(command, cancellationToken), id => sourceId = id);
            }

            return sourceId;
        }

        private Task<IReadOnlyCollection<IDomainEvent>> ExecuteCommandAsync<TAggregate, TIdentity, TSourceIdentity>(
            ICommand<TAggregate, TIdentity, TSourceIdentity> command,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TSourceIdentity : ISourceId
        {
            var commandType = command.GetType();
            var commandExecutionDetails = GetCommandExecutionDetails(commandType);

            var commandHandlers = _resolver.ResolveAll(commandExecutionDetails.CommandHandlerType).ToList();
            if (!commandHandlers.Any())
            {
                throw new NoCommandHandlersException(string.Format(
                    "No command handlers registered for the command '{0}' on aggregate '{1}'",
                    commandType.PrettyPrint(),
                    commandExecutionDetails.AggregateType.PrettyPrint()));
            }
            if (commandHandlers.Count > 1)
            {
                throw new InvalidOperationException(string.Format(
                    "Too many command handlers the command '{0}' on aggregate '{1}'. These were found: {2}",
                    commandType.PrettyPrint(),
                    commandExecutionDetails.AggregateType.PrettyPrint(),
                    string.Join(", ", commandHandlers.Select(h => h.GetType().PrettyPrint()))));
            }

            var commandHandler = (ICommandHandler) commandHandlers.Single();

            return _transientFaultHandler.TryAsync(
                async c =>
                    {
                        var aggregate = await _eventStore.LoadAggregateAsync<TAggregate, TIdentity>(command.AggregateId, c).ConfigureAwait(false);
                        if (aggregate.HasSourceId(command.SourceId))
                        {
                            throw new DuplicateOperationException(
                                command.SourceId,
                                aggregate.Id,
                                $"Aggregate '{aggregate.GetType().PrettyPrint()}' has already had operation '{command.SourceId}' performed. New source is '{command.GetType().PrettyPrint()}'");
                        }

                        await commandExecutionDetails.Invoker(commandHandler, aggregate, command, c).ConfigureAwait(false);
                        return await aggregate.CommitAsync(_eventStore, command.SourceId, c).ConfigureAwait(false);
                    },
                Label.Named($"command-execution-{commandType.Name.ToLowerInvariant()}"), 
                cancellationToken);
        }

        private class CommandExecutionDetails
        {
            public Type AggregateType { get; set; }
            public Type CommandHandlerType { get; set; }
            public Func<ICommandHandler, IAggregateRoot, ICommand, CancellationToken, Task> Invoker { get; set; } 
        }

        private static CommandExecutionDetails GetCommandExecutionDetails(Type commandType)
        {
            return CommandExecutionDetailsMap.GetOrAdd(
                commandType,
                t =>
                    {
                        var commandInterfaceType = t
                            .GetInterfaces()
                            .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (ICommand<,,>));
                        var commandTypes = commandInterfaceType.GetGenericArguments();

                        var commandHandlerType = typeof(ICommandHandler<,,,>)
                            .MakeGenericType(commandTypes[0], commandTypes[1], commandTypes[2], commandType);

                        var invoker = commandHandlerType.GetMethod("ExecuteAsync");

                        return new CommandExecutionDetails
                            {
                                AggregateType = commandTypes[0],
                                CommandHandlerType = commandHandlerType,
                                Invoker = ((h, a, command, c) => (Task)invoker.Invoke(h, new object[] { a, command, c }))
                            };
                    });
        }
    }
}
