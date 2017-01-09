// The MIT License (MIT)
//
// Copyright (c) 2015-2017 Rasmus Mikkelsen
// Copyright (c) 2015-2017 eBay Software Foundation
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
using EventFlow.Core;

namespace EventFlow.Sagas.AggregateSagas
{
    public abstract class AggregateSaga<TSaga, TIdentity, TLocator> : AggregateRoot<TSaga, TIdentity>, IAggregateSaga<TIdentity, TLocator>
        where TSaga : AggregateSaga<TSaga, TIdentity, TLocator>
        where TIdentity : ISagaId
        where TLocator : ISagaLocator
    {
        private readonly ICollection<Func<ICommandBus, CancellationToken, Task>> _unpublishedCommands = new List<Func<ICommandBus, CancellationToken, Task>>();

        private bool _isCompleted;

        protected AggregateSaga(TIdentity id) : base(id)
        {
        }

        protected void Complete()
        {
            _isCompleted = true;
        }

        protected void Publish<TCommandAggregate, TCommandAggregateIdentity, TCommandSourceIdentity>(
            ICommand<TCommandAggregate, TCommandAggregateIdentity, TCommandSourceIdentity> command)
            where TCommandAggregate : IAggregateRoot<TCommandAggregateIdentity>
            where TCommandAggregateIdentity : IIdentity
            where TCommandSourceIdentity : ISourceId
        {
            _unpublishedCommands.Add((b, c) => b.PublishAsync(command, c));
        }

        public SagaState State => _isCompleted
            ? SagaState.Completed
            : IsNew ? SagaState.New : SagaState.Running;

        public async Task PublishAsync(ICommandBus commandBus, CancellationToken cancellationToken)
        {
            foreach (var unpublishedCommand in _unpublishedCommands.ToList())
            {
                await unpublishedCommand(commandBus, cancellationToken).ConfigureAwait(false);
                _unpublishedCommands.Remove(unpublishedCommand);
            }
        }
    }
}