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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Logs;

namespace EventFlow.ReadStores.InMemory
{
    public class InMemoryReadModelStore<TAggregate, TReadModel> :
        ReadModelStore<TAggregate, TReadModel>,
        IInMemoryReadModelStore<TAggregate, TReadModel>
        where TReadModel : IReadModel, new()
        where TAggregate : IAggregateRoot
    {
        private readonly Dictionary<string, TReadModel> _readModels = new Dictionary<string, TReadModel>();

        public InMemoryReadModelStore(
            ILog log)
            : base(log)
        {
        }

        public override Task UpdateReadModelAsync(
            IAggregateId id,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            TReadModel readModel;
            if (_readModels.ContainsKey(id.Value))
            {
                readModel = _readModels[id.Value];
            }
            else
            {
                readModel = new TReadModel();
                _readModels.Add(id.Value, readModel);
            }

            ApplyEvents(readModel, domainEvents);

            return Task.FromResult(0);
        }

        public TReadModel Get(IAggregateId id)
        {
            TReadModel readModel;
            return _readModels.TryGetValue(id.Value, out readModel)
                ? readModel
                : default(TReadModel);
        }

        public IEnumerable<TReadModel> GetAll()
        {
            return _readModels.Values;
        }

        public IEnumerable<TReadModel> Find(Func<TReadModel, bool> predicate)
        {
            return _readModels.Values
                .Where(predicate);
        } 
    }
}
