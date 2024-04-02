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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Extensions;
using Microsoft.Extensions.Logging;

namespace EventFlow.Snapshots.Stores.InMemory
{
    public class InMemorySnapshotPersistence : ISnapshotPersistence
    {
        private readonly ILogger<InMemorySnapshotPersistence> _logger;
        private readonly AsyncLock _asyncLock = new AsyncLock();
        private readonly Dictionary<Type, Dictionary<string, CommittedSnapshot>> _snapshots = new Dictionary<Type, Dictionary<string, CommittedSnapshot>>();

        public InMemorySnapshotPersistence(
            ILogger<InMemorySnapshotPersistence> logger)
        {
            _logger = logger;
        }

        public async Task<CommittedSnapshot> GetSnapshotAsync(
            Type aggregateType,
            IIdentity identity,
            CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!_snapshots.TryGetValue(aggregateType, out var snapshots))
                {
                    return null;
                }

                return snapshots.TryGetValue(identity.Value, out var committedSnapshot)
                    ? committedSnapshot
                    : null;
            }
        }

        public async Task SetSnapshotAsync(
            Type aggregateType,
            IIdentity identity,
            SerializedSnapshot serializedSnapshot,
            CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                _logger.LogTrace(
                    "Setting snapshot {AggregateType} with ID {Id}",
                    aggregateType.PrettyPrint(),
                    identity.Value);

                if (!_snapshots.TryGetValue(aggregateType, out var snapshots))
                {
                    snapshots = new Dictionary<string, CommittedSnapshot>();
                    _snapshots[aggregateType] = snapshots;
                }

                snapshots[identity.Value] = serializedSnapshot;
            }
        }

        public async Task DeleteSnapshotAsync(
            Type aggregateType,
            IIdentity identity,
            CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                _logger.LogTrace(
                    "Deleting snapshot {AggregateType} with ID {Id}",
                    aggregateType.PrettyPrint(),
                    identity.Value);

                if (!_snapshots.TryGetValue(aggregateType, out var snapshots))
                {
                    return;
                }

                snapshots.Remove(identity.Value);
            }
        }

        public async Task PurgeSnapshotsAsync(
            Type aggregateType,
            CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                _logger.LogWarning(
                    "Purging ALL snapshots of type {AggregateType}!",
                    aggregateType.PrettyPrint());

                _snapshots.Remove(aggregateType);
            }
        }

        public async Task PurgeSnapshotsAsync(
            CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                _logger.LogWarning("Purging ALL snapshots!");

                _snapshots.Clear();
            }
        }
    }
}
