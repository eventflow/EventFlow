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
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Extensions;

namespace EventFlow.ResilienceStrategies
{
    public class CreateAndDeleteStateEnforcedResilienceStrategy : IAggregateStoreResilienceStrategy
    {
        public CreateAndDeleteStateEnforcedResilienceStrategy()
        {
        }

        public Task BeforeAggregateLoad<TAggregate, TIdentity, TExecutionResult>(
            TIdentity id,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            return Task.CompletedTask;
        }

        public Task BeforeAggregateUpdate<TAggregate, TIdentity, TExecutionResult>(
            TAggregate aggregate,
            Func<TAggregate, CancellationToken, Task<TExecutionResult>> updateAggregate, 
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            var commandType = GetCommandType<TAggregate, TIdentity, TExecutionResult>(updateAggregate);
            var isInitatorCommand = typeof(ICommandInitator).IsAssignableFrom(commandType);

            var actionOnDeletedAggregate = aggregate.IsDeleted;
            if (actionOnDeletedAggregate)
            {
                throw new InvalidOperationException($"Aggregate '{typeof(TAggregate).PrettyPrint()}' had command '{commandType.PrettyPrint()}' when it was in a deleted state.");
            }

            var aggregateExistButShouldnt = !aggregate.IsNew && isInitatorCommand;
            if (aggregateExistButShouldnt)
            {
                throw new InvalidOperationException($"Aggregate '{typeof(TAggregate).PrettyPrint()}' had initator command '{commandType.PrettyPrint()}' when it already has a state");
            }

            var aggregateDoesntExistButShould = aggregate.IsNew && !isInitatorCommand;
            if (aggregateDoesntExistButShould)
            {
                throw new InvalidOperationException($"Aggregate '{typeof(TAggregate).PrettyPrint()}' had non-initator command '{commandType.PrettyPrint()}' when it doesn't have state");
            }

            return Task.CompletedTask;
        }

        public Task BeforeCommitAsync<TAggregate, TIdentity, TExecutionResult>(TAggregate aggregate,
            TExecutionResult executionResult,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            return Task.CompletedTask;
        }

        public Task<(bool, IAggregateUpdateResult<TExecutionResult>)> HandleCommitFailedAsync<TAggregate, TIdentity,
            TExecutionResult>(TAggregate aggregate,
            TExecutionResult executionResult,
            Exception exception,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            return Task.FromResult<(bool, IAggregateUpdateResult<TExecutionResult>)>((false, null));
        }

        public Task CommitSucceededAsync<TAggregate, TIdentity, TExecutionResult>(TAggregate aggregate,
            TExecutionResult executionResult,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            return Task.CompletedTask;
        }

        public Task EventPublishSkippedAsync<TAggregate, TIdentity, TExecutionResult>(TIdentity id,
            TExecutionResult executionResult,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            return Task.CompletedTask;
        }

        public Task BeforeEventPublishAsync<TAggregate, TIdentity, TExecutionResult>(TIdentity id,
            TExecutionResult executionResult,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            return Task.CompletedTask;
        }

        public Task<bool> HandleEventPublishFailedAsync<TAggregate, TIdentity, TExecutionResult>(TIdentity id,
            TExecutionResult executionResult,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            Exception exception,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            return Task.FromResult(false);
        }

        public Task EventPublishSucceededAsync<TAggregate, TIdentity, TExecutionResult>(TIdentity id,
            TExecutionResult executionResult,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            return Task.CompletedTask;
        }

        private Type GetCommandType<TAggregate, TIdentity, TExecutionResult>(Func<TAggregate, CancellationToken, Task<TExecutionResult>> updateAggregate)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            var targetMethod = updateAggregate.Target.GetType();
            var commandField = targetMethod.GetFields().Last();
            return commandField.GetValue(updateAggregate.Target).GetType();
        }
    }
}
