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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Core;
using EventFlow.Shims;

namespace EventFlow.EventStores
{
    public class NoAggregateStoreResilienceStrategy : IAggregateStoreResilienceStrategy
    {
        public Task BeforeAggregateUpdate<TAggregate, TIdentity, TExecutionResult>(
            TIdentity id,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            return Tasks.Completed;
        }

        public Task BeforeCommitAsync<TAggregate, TIdentity, TExecutionResult>(TAggregate aggregate,
            TExecutionResult executionResult,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            return Tasks.Completed;
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
            return Tasks.Completed;
        }

        public Task EventPublishSkippedAsync<TAggregate, TIdentity, TExecutionResult>(TIdentity id,
            TExecutionResult executionResult,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            return Tasks.Completed;
        }

        public Task BeforeEventPublishAsync<TAggregate, TIdentity, TExecutionResult>(TIdentity id,
            TExecutionResult executionResult,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            return Tasks.Completed;
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
            return Tasks.Completed;
        }
    }
}
