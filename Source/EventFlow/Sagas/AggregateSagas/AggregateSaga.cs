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
using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using EventFlow.Core;
using EventFlow.Exceptions;
using EventFlow.Extensions;

namespace EventFlow.Sagas.AggregateSagas
{
    public abstract class AggregateSaga<TSaga, TIdentity, TLocator> : AggregateRoot<TSaga, TIdentity>, IAggregateSaga<TIdentity, TLocator>
        where TSaga : AggregateSaga<TSaga, TIdentity, TLocator>
        where TIdentity : ISagaId
        where TLocator : ISagaLocator
    {
        private readonly ICollection<Tuple<ICommand, Func<ICommandBus, CancellationToken, Task<IExecutionResult>>>> _unpublishedCommands = new List<Tuple<ICommand, Func<ICommandBus, CancellationToken, Task<IExecutionResult>>>>();

        private bool _isCompleted;

        protected virtual bool ThrowExceptionsOnFailedPublish { get; set; } = true;

        protected AggregateSaga(TIdentity id) : base(id)
        {
        }

        protected void Complete()
        {
            _isCompleted = true;
        }

        protected void Publish<TCommandAggregate, TCommandAggregateIdentity, TExecutionResult>(
            ICommand<TCommandAggregate, TCommandAggregateIdentity, TExecutionResult> command)
            where TCommandAggregate : IAggregateRoot<TCommandAggregateIdentity>
            where TCommandAggregateIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            _unpublishedCommands.Add(
                new Tuple<ICommand, Func<ICommandBus, CancellationToken, Task<IExecutionResult>>>(
                        command,
                        async (b, c) => await b.PublishAsync(command, c).ConfigureAwait(false))
                );
        }

        public SagaState State => _isCompleted
            ? SagaState.Completed
            : IsNew ? SagaState.New : SagaState.Running;

        public virtual async Task PublishAsync(ICommandBus commandBus, CancellationToken cancellationToken)
        {
            var commandsToPublish = _unpublishedCommands.ToList();
            _unpublishedCommands.Clear();

            var exceptions = new List<CommandException>();
            foreach (var unpublishedCommand in commandsToPublish)
            {
                var command = unpublishedCommand.Item1;
                var commandInvoker = unpublishedCommand.Item2;
                if (ThrowExceptionsOnFailedPublish)
                {
                    try
                    {
                        var executionResult = await commandInvoker(commandBus, cancellationToken).ConfigureAwait(false);
                        if (executionResult?.IsSuccess == false)
                        {
                            exceptions.Add(
                                new CommandException(
                                    command.GetType(),
                                    command.GetSourceId(),
                                    executionResult,
                                    $"Command '{command.GetType().PrettyPrint()}' with ID '{command.GetSourceId()}' published from a saga with ID '{Id}' failed with: '{executionResult}'. See ExecutionResult."));
                        }
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(
                            new CommandException(
                                command.GetType(),
                                command.GetSourceId(),
                                $"Command '{command.GetType().PrettyPrint()}' with ID '{command.GetSourceId()}' published from a saga with ID '{Id}' failed with: '{e.Message}'. See InnerException.",
                                e));
                    }
                }
                else
                {
                    await commandInvoker(commandBus, cancellationToken).ConfigureAwait(false);
                }
            }

            if (0 < exceptions.Count)
            {
                throw new SagaPublishException(
                    $"Some commands published from a saga with ID '{Id}' failed. See InnerExceptions.",
                    exceptions);
            }
        }
    }
}
