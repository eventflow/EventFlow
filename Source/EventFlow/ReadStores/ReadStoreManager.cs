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
using EventFlow.Logs;

namespace EventFlow.ReadStores
{
    public class ReadStoreManager : IReadStoreManager
    {
        private readonly ILog _log;
        private readonly IResolver _resolver;

        public ReadStoreManager(
            ILog log,
            IResolver resolver)
        {
            _log = log;
            _resolver = resolver;
        }

        public async Task UpdateReadStoresAsync<TAggregate>(
            string id,
            IReadOnlyCollection<IDomainEvent> domainEvents)
            where TAggregate : IAggregateRoot
        {
            var readModelStores = _resolver.Resolve<IEnumerable<IReadModelStore<TAggregate>>>().ToList();
            var updateTasks = readModelStores
                .Select(s => UpdateReadStoreAsync(s, id, domainEvents))
                .ToArray();
            await Task.WhenAll(updateTasks).ConfigureAwait(false);
        }

        private async Task UpdateReadStoreAsync<TAggregate>(
            IReadModelStore<TAggregate> readModelStore,
            string id,
            IReadOnlyCollection<IDomainEvent> domainEvents)
            where TAggregate : IAggregateRoot
        {
            var readModelStoreType = readModelStore.GetType();
            var aggregateType = typeof(TAggregate);

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
