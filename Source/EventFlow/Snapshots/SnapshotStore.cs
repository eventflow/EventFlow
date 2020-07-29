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

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.Snapshots.Stores;

namespace EventFlow.Snapshots
{
    public class SnapshotStore : ISnapshotStore
    {
        private readonly ILog _log;
        private readonly ISnapshotSerilizer _snapshotSerilizer;
        private readonly ISnapshotPersistence _snapshotPersistence;

        public SnapshotStore(
            ILog log,
            ISnapshotSerilizer snapshotSerilizer,
            ISnapshotPersistence snapshotPersistence)
        {
            _log = log;
            _snapshotSerilizer = snapshotSerilizer;
            _snapshotPersistence = snapshotPersistence;
        }

        public async Task<SnapshotContainer> LoadSnapshotAsync<TAggregate, TIdentity, TSnapshot>(
            TIdentity identity,
            CancellationToken cancellationToken)
            where TAggregate : ISnapshotAggregateRoot<TIdentity, TSnapshot>
            where TIdentity : IIdentity
            where TSnapshot : ISnapshot
        {
            _log.Verbose(() => $"Fetching snapshot for '{typeof(TAggregate).PrettyPrint()}' with ID '{identity}'");
            var committedSnapshot = await _snapshotPersistence.GetSnapshotAsync(
                typeof(TAggregate),
                identity,
                cancellationToken)
                .ConfigureAwait(false);
            if (committedSnapshot == null)
            {
                _log.Verbose(() => $"No snapshot found for '{typeof(TAggregate).PrettyPrint()}' with ID '{identity}'");
                return null;
            }

            var snapshotContainer = await _snapshotSerilizer.DeserializeAsync<TAggregate, TIdentity, TSnapshot>(
                committedSnapshot,
                cancellationToken)
                .ConfigureAwait(false);

            return snapshotContainer;
        }

        public async Task StoreSnapshotAsync<TAggregate, TIdentity, TSnapshot>(
            TIdentity identity,
            SnapshotContainer snapshotContainer,
            CancellationToken cancellationToken)
            where TAggregate : ISnapshotAggregateRoot<TIdentity, TSnapshot>
            where TIdentity : IIdentity
            where TSnapshot : ISnapshot
        {
            var serializedSnapshot = await _snapshotSerilizer.SerilizeAsync<TAggregate, TIdentity, TSnapshot>(
                snapshotContainer,
                cancellationToken)
                .ConfigureAwait(false);

            await _snapshotPersistence.SetSnapshotAsync(
                typeof(TAggregate),
                identity,
                serializedSnapshot,
                cancellationToken)
                .ConfigureAwait(false);
        }
    }
}