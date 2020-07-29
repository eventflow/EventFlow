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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Core.Caching;
using EventFlow.Exceptions;
using EventFlow.Extensions;
using EventFlow.Logs;

namespace EventFlow
{
    public class CommandBus : ICommandBus
    {
        private readonly ILog _log;
        private readonly IResolver _resolver;
        private readonly IAggregateStore _aggregateStore;
        private readonly IMemoryCache _memoryCache;

        public CommandBus(
            ILog log,
            IResolver resolver,
            IAggregateStore aggregateStore,
            IMemoryCache memoryCache)
        {
            _log = log;
            _resolver = resolver;
            _aggregateStore = aggregateStore;
            _memoryCache = memoryCache;
        }

        public async Task<TResult> PublishAsync<TAggregate, TIdentity, TResult>(
            ICommand<TAggregate, TIdentity, TResult> command,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TResult : IExecutionResult
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            _log.Verbose(() => $"Executing command '{command.GetType().PrettyPrint()}' with ID '{command.SourceId}' on aggregate '{typeof(TAggregate).PrettyPrint()}'");

            IAggregateUpdateResult<TResult> aggregateUpdateResult;
            try
            {
                aggregateUpdateResult = await ExecuteCommandAsync(command, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _log.Debug(
                    exception,
                    "Execution of command '{0}' with ID '{1}' on aggregate '{2}' failed due to exception '{3}' with message: {4}",
                    command.GetType().PrettyPrint(),
                    command.SourceId,
                    typeof(TAggregate),
                    exception.GetType().PrettyPrint(),
                    exception.Message);
                throw;
            }

            _log.Verbose(() => !aggregateUpdateResult.DomainEvents.Any()
                ? string.Format(
                    "Execution command '{0}' with ID '{1}' on aggregate '{2}' did NOT result in any domain events, was success:{3}",
                    command.GetType().PrettyPrint(),
                    command.SourceId,
                    typeof(TAggregate).PrettyPrint(),
                    aggregateUpdateResult.Result?.IsSuccess)
                : string.Format(
                    "Execution command '{0}' with ID '{1}' on aggregate '{2}' resulted in these events: {3}, was success: {4}",
                    command.GetType().PrettyPrint(),
                    command.SourceId,
                    typeof(TAggregate),
                    string.Join(", ", aggregateUpdateResult.DomainEvents.Select(d => d.EventType.PrettyPrint())),
                    aggregateUpdateResult.Result?.IsSuccess));

            return aggregateUpdateResult.Result;
        }

        private async Task<IAggregateUpdateResult<TResult>> ExecuteCommandAsync<TAggregate, TIdentity, TResult>(
            ICommand<TAggregate, TIdentity, TResult> command,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TResult : IExecutionResult
        {
            var commandType = command.GetType();
            var commandExecutionDetails = await GetCommandExecutionDetailsAsync(commandType, cancellationToken).ConfigureAwait(false);

            var commandHandlers = _resolver.ResolveAll(commandExecutionDetails.CommandHandlerType)
                .Cast<ICommandHandler>()
                .ToList();
            if (!commandHandlers.Any())
            {
                throw new NoCommandHandlersException(string.Format(
                    "No command handlers registered for the command '{0}' on aggregate '{1}'",
                    commandType.PrettyPrint(),
                    typeof(TAggregate).PrettyPrint()));
            }
            if (commandHandlers.Count > 1)
            {
                throw new InvalidOperationException(string.Format(
                    "Too many command handlers the command '{0}' on aggregate '{1}'. These were found: {2}",
                    commandType.PrettyPrint(),
                    typeof(TAggregate).PrettyPrint(),
                    string.Join(", ", commandHandlers.Select(h => h.GetType().PrettyPrint()))));
            }

            var commandHandler = commandHandlers.Single();

            return await _aggregateStore.UpdateAsync<TAggregate, TIdentity, TResult>(
                command.AggregateId,
                command.SourceId,
                (a, c) => (Task<TResult>) commandExecutionDetails.Invoker(commandHandler, a, command, c),
                cancellationToken)
                .ConfigureAwait(false);
        }

        private class CommandExecutionDetails
        {
            public Type CommandHandlerType { get; set; }
            public Func<ICommandHandler, IAggregateRoot, ICommand, CancellationToken, Task> Invoker { get; set; } 
        }

        private const string NameOfExecuteCommand = nameof(
            ICommandHandler<
                IAggregateRoot<IIdentity>,
                IIdentity,
                IExecutionResult,
                ICommand<IAggregateRoot<IIdentity>, IIdentity, IExecutionResult>
            >.ExecuteCommandAsync);
        private Task<CommandExecutionDetails> GetCommandExecutionDetailsAsync(Type commandType, CancellationToken cancellationToken)
        {
            return _memoryCache.GetOrAddAsync(
                CacheKey.With(GetType(), commandType.GetCacheKey()),
                TimeSpan.FromDays(1), 
                _ =>
                    {
                        var commandInterfaceType = commandType
                            .GetTypeInfo()
                            .GetInterfaces()
                            .Single(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<,,>));
                        var commandTypes = commandInterfaceType.GetTypeInfo().GetGenericArguments();

                        var commandHandlerType = typeof(ICommandHandler<,,,>)
                            .MakeGenericType(commandTypes[0], commandTypes[1], commandTypes[2], commandType);
                        
                        _log.Verbose(() => $"Command '{commandType.PrettyPrint()}' is resolved by '{commandHandlerType.PrettyPrint()}'");

                        var invokeExecuteAsync = ReflectionHelper.CompileMethodInvocation<Func<ICommandHandler, IAggregateRoot, ICommand, CancellationToken, Task>>(
                            commandHandlerType, NameOfExecuteCommand);

                        return Task.FromResult(new CommandExecutionDetails
                            {
                                CommandHandlerType = commandHandlerType,
                                Invoker = invokeExecuteAsync
                            });
                    },
                cancellationToken);
        }
    }
}