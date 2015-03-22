// The MIT License (MIT)
//
// Copyright (c) 2015 EventFlow
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
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Logs;
using EventFlow.ReadStores;

namespace EventFlow
{
    public class CommandBus : ICommandBus
    {
        private readonly ILog _log;
        private readonly IResolver _resolver;
        private readonly IEventStore _eventStore;
        private readonly IDispatchToEventHandlers _dispatchToEventHandlers;

        public CommandBus(
            ILog log,
            IResolver resolver,
            IEventStore eventStore,
            IDispatchToEventHandlers dispatchToEventHandlers)
        {
            _log = log;
            _resolver = resolver;
            _eventStore = eventStore;
            _dispatchToEventHandlers = dispatchToEventHandlers;
        }

        public async Task PublishAsync<TAggregate>(ICommand<TAggregate> command)
            where TAggregate : IAggregateRoot
        {
            var commandType = command.GetType().Name;
            var aggregateType = typeof (TAggregate);
            _log.Verbose(
                "Executing command '{0}' on aggregate '{1}' with ID '{2}'",
                commandType,
                aggregateType.Name,
                command.Id);

            var aggregate = await _eventStore.LoadAggregateAsync<TAggregate>(command.Id).ConfigureAwait(false);
            await command.ExecuteAsync(aggregate).ConfigureAwait(false);
            var domainEvents = await aggregate.CommitAsync(_eventStore).ConfigureAwait(false);
            if (!domainEvents.Any())
            {
                _log.Verbose(
                    "Execution command '{0}' on aggregate '{1}' with ID '{2}' did NOT result in any domain events",
                    commandType,
                    aggregateType.Name,
                    command.Id);
                return;
            }

            await UpdateReadModelStoresAsync<TAggregate>(command.Id, domainEvents).ConfigureAwait(false);
            await _dispatchToEventHandlers.DispatchAsync(domainEvents).ConfigureAwait(false);
        }

        public void Publish<TAggregate>(ICommand<TAggregate> command) where TAggregate : IAggregateRoot
        {
            using (var a = AsyncHelper.Wait)
            {
                a.Run(PublishAsync(command));
            }
        }

        private async Task UpdateReadModelStoresAsync<TAggregate>(
            string id,
            IReadOnlyCollection<IDomainEvent> domainEvents)
            where TAggregate : IAggregateRoot
        {
            var readModelStores = _resolver.Resolve<IEnumerable<IReadModelStore<TAggregate>>>().ToList();
            var updateTasks = readModelStores
                .Select(s => UpdateReadModelStoreAsync(s, id, domainEvents))
                .ToArray();
            await Task.WhenAll(updateTasks).ConfigureAwait(false);
        }

        private async Task UpdateReadModelStoreAsync<TAggregate>(
            IReadModelStore<TAggregate> readModelStore,
            string id,
            IReadOnlyCollection<IDomainEvent> domainEvents)
            where TAggregate : IAggregateRoot
        {
            var readModelStoreType = readModelStore.GetType();
            var aggregateType = typeof (TAggregate);

            _log.Verbose(
                "Updating read model store '{0}' for aggregate '{1}' with '{2}' by applying {3} events",
                readModelStoreType.Name,
                aggregateType.Name,
                id,
                domainEvents.Count);

            try
            {
                await readModelStore.UpdateReadModelAsync(id, domainEvents).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _log.Error(
                    exception,
                    "Failed to updated read model store {0}",
                    readModelStoreType.Name);
            }
        }
    }
}
