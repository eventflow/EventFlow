// The MIT License (MIT)
//
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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
using EventFlow.Snapshots.Stores;
using Microsoft.Extensions.Logging;

namespace EventFlow.Snapshots
{
    public class SnapshotStore : ISnapshotStore
    {
        private readonly ILogger<SnapshotStore> _logger;
        private readonly ISnapshotSerializer _snapshotSerializer;
        private readonly ISnapshotPersistence _snapshotPersistence;

        public SnapshotStore(
            ILogger<SnapshotStore> logger,
            ISnapshotSerializer snapshotSerializer,
            ISnapshotPersistence snapshotPersistence)
        {
            _logger = logger;
            _snapshotSerializer = snapshotSerializer;
            _snapshotPersistence = snapshotPersistence;
        }

        public async Task<SnapshotContainer> LoadSnapshotAsync<TAggregate, TIdentity, TSnapshot>(
            TIdentity identity,
            CancellationToken cancellationToken)
            where TAggregate : ISnapshotAggregateRoot<TIdentity, TSnapshot>
            where TIdentity : IIdentity
            where TSnapshot : ISnapshot
        {
            _logger.LogTrace(
                "Fetching snapshot for {AggregateType} with ID {Id}",
                typeof(TAggregate).PrettyPrint(),
                identity);
            var committedSnapshot = await _snapshotPersistence.GetSnapshotAsync(
                typeof(TAggregate),
                identity,
                cancellationToken)
                .ConfigureAwait(false);
            if (committedSnapshot == null)
            {
                _logger.LogTrace(
                    "No snapshot found for {AggregateType} with ID {Id}",
                    typeof(TAggregate).PrettyPrint(),
                    identity);
                return null;
            }

            var snapshotContainer = await _snapshotSerializer.DeserializeAsync<TAggregate, TIdentity, TSnapshot>(
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
            var serializedSnapshot = await _snapshotSerializer.SerializeAsync<TAggregate, TIdentity, TSnapshot>(
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
