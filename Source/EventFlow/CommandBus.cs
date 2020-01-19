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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using EventFlow.Core;
using EventFlow.Exceptions;
using EventFlow.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventFlow
{
    public class CommandBus : ICommandBus
    {
        private readonly ILogger<CommandBus> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IAggregateStore _aggregateStore;
        private readonly IMemoryCache _memoryCache;

        public CommandBus(
            ILogger<CommandBus> logger,
            IServiceProvider serviceProvider,
            IAggregateStore aggregateStore,
            IMemoryCache memoryCache)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
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
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            using (_logger.BeginScope(new Dictionary<string, object>
                {
                    ["CommandTypeName"] = command.GetType().PrettyPrint(),
                    ["AggregateTypeName"] = typeof(TAggregate).PrettyPrint(),
                    ["CommandSourceId"] = command.SourceId.Value,
                }))
            {
                IAggregateUpdateResult<TResult> aggregateUpdateResult;
                try
                {
                    aggregateUpdateResult = await ExecuteCommandAsync(command, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _logger.LogDebug(
                        exception,
                        "Execution of command failed due to exception {ExceptionType} with message: {ExceptionMessage}",
                        exception.GetType().PrettyPrint(),
                        exception.Message);

                    throw;
                }

                _logger.DoIfLogLevel(
                    LogLevel.Trace,
                    l =>
                    {
                        if (aggregateUpdateResult.DomainEvents.Any())
                        {
                            l.LogTrace(
                                "Execution command resulted in these events: {DomainEvents}, was success: {WasSuccess}",
                                string.Join(", ", aggregateUpdateResult.DomainEvents.Select(d => d.EventType.PrettyPrint())),
                                aggregateUpdateResult.Result?.IsSuccess);
                        }
                        else
                        {
                            l.LogTrace(
                                "Execution command did NOT result in any domain events, was success: {WasSuccess}", 
                                aggregateUpdateResult.Result?.IsSuccess);
                        }
                    });

                return aggregateUpdateResult.Result;
            }
        }

        private async Task<IAggregateUpdateResult<TResult>> ExecuteCommandAsync<TAggregate, TIdentity, TResult>(
            ICommand<TAggregate, TIdentity, TResult> command,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TResult : IExecutionResult
        {
            var commandType = command.GetType();
            var commandExecutionDetails = GetCommandExecutionDetails(commandType);

            var commandHandlers = _serviceProvider.GetServices(commandExecutionDetails.CommandHandlerType)
                .Cast<ICommandHandler>()
                .ToList();
            if (!commandHandlers.Any())
            {
                throw new NoCommandHandlersException(
                    $"No command handlers registered for the command '{commandType.PrettyPrint()}' " +
                    $"on aggregate '{typeof(TAggregate).PrettyPrint()}'");
            }
            if (commandHandlers.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Too many command handlers the command '{commandType.PrettyPrint()}' on " +
                    $"aggregate '{typeof(TAggregate).PrettyPrint()}'. These were found: " +
                    $"{string.Join(", ", commandHandlers.Select(h => h.GetType().PrettyPrint()))}");
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

        private CommandExecutionDetails GetCommandExecutionDetails(Type commandType)
        {
            return _memoryCache.GetOrCreate(
                commandType,
                e =>
                    {
                        var commandInterfaceType = commandType
                            .GetTypeInfo()
                            .GetInterfaces()
                            .Single(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<,,>));
                        var commandTypes = commandInterfaceType.GetTypeInfo().GetGenericArguments();

                        var commandHandlerType = typeof(ICommandHandler<,,,>)
                            .MakeGenericType(commandTypes[0], commandTypes[1], commandTypes[2], commandType);

                        var invokeExecuteAsync = ReflectionHelper.CompileMethodInvocation<Func<ICommandHandler, IAggregateRoot, ICommand, CancellationToken, Task>>(
                            commandHandlerType, NameOfExecuteCommand);

                        return new CommandExecutionDetails
                            {
                                CommandHandlerType = commandHandlerType,
                                Invoker = invokeExecuteAsync
                            };
                    });
        }
    }
}
