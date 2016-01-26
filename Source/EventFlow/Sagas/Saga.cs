// The MIT License (MIT)
//
// Copyright (c) 2015-2016 Rasmus Mikkelsen
// Copyright (c) 2015-2016 eBay Software Foundation
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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Core;
using EventFlow.EventStores;

namespace EventFlow.Sagas
{
    public abstract class Saga<TSaga, TIdentity, TLocator> : AggregateRoot<TSaga, TIdentity>, ISaga<TIdentity, TLocator>
        where TSaga : Saga<TSaga, TIdentity, TLocator>
        where TIdentity : ISagaId
        where TLocator : ISagaLocator
    {
        private readonly ICollection<Func<ICommandBus, CancellationToken, Task>> _unpublishedCommands = new List<Func<ICommandBus, CancellationToken, Task>>();

        public static async Task<ISaga> LoadSagaAsync(IEventStore eventStore, TIdentity identity, CancellationToken cancellationToken)
        {
            return await eventStore.LoadAggregateAsync<TSaga, TIdentity>(identity, cancellationToken).ConfigureAwait(false);
        }

        protected Saga(TIdentity id) : base(id)
        {
        }

        protected void Schedule<TCommandAggregate, TCommandAggregateIdentity, TCommandSourceIdentity>(
            ICommand<TCommandAggregate, TCommandAggregateIdentity, TCommandSourceIdentity> command)
            where TCommandAggregate : IAggregateRoot<TCommandAggregateIdentity>
            where TCommandAggregateIdentity : IIdentity
            where TCommandSourceIdentity : ISourceId
        {
            _unpublishedCommands.Add((b, c) => b.PublishAsync(command, c));
        }

        public async Task PublishAsync(ICommandBus commandBus, CancellationToken cancellationToken)
        {
            foreach (var unpublishedCommand in _unpublishedCommands)
            {
                await unpublishedCommand(commandBus, cancellationToken).ConfigureAwait(false);
            }
            _unpublishedCommands.Clear();
        }
    }
}
