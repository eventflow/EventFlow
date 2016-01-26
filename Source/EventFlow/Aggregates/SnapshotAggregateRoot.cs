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
// 

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.EventStores.Snapshots;

namespace EventFlow.Aggregates
{
    public abstract class SnapshotAggregateRoot<TSnapshot, TAggregate, TIdentity> : AggregateRoot<TAggregate, TIdentity>,
        ISnapshotAggregateRoot<TSnapshot, TIdentity>
        where TAggregate : AggregateRoot<TAggregate, TIdentity>
        where TIdentity : IIdentity
        where TSnapshot : ISnapshot
    {
        protected SnapshotAggregateRoot(
            TIdentity id)
            : base(id)
        {
        }

        public async Task<ISnapshot> CreateSnapshotAsync(CancellationToken cancellationToken)
        {
            var snapshot = await InternalCreateSnapshotAsync(cancellationToken).ConfigureAwait(false);

            // TODO: Enrich snapshot

            return snapshot;
        }

        public Task LoadSnapshotAsyncAsync(ISnapshot snapshot, CancellationToken cancellationToken)
        {
            return InternalLoadSnapshotAsync((TSnapshot) snapshot, cancellationToken);
        }

        protected abstract Task<TSnapshot> InternalCreateSnapshotAsync(CancellationToken cancellationToken);

        protected abstract Task InternalLoadSnapshotAsync(TSnapshot snapshot, CancellationToken cancellationToken);
    }
}