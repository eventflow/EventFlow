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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.Logs;

namespace EventFlow.Snapshots
{
    public class SnapshotSerilizer : ISnapshotSerilizer
    {
        private readonly ILog _log;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ISnapshotUpgradeService _snapshotUpgradeService;
        private readonly ISnapshotDefinitionService _snapshotDefinitionService;

        public SnapshotSerilizer(
            ILog log,
            IJsonSerializer jsonSerializer,
            ISnapshotUpgradeService snapshotUpgradeService,
            ISnapshotDefinitionService snapshotDefinitionService)
        {
            _log = log;
            _jsonSerializer = jsonSerializer;
            _snapshotUpgradeService = snapshotUpgradeService;
            _snapshotDefinitionService = snapshotDefinitionService;
        }

        public Task<SerializedSnapshot> SerilizeAsync<TAggregate, TIdentity, TSnapshot>(
            SnapshotContainer snapshotContainer,
            CancellationToken cancellationToken)
            where TAggregate : ISnapshotAggregateRoot<TIdentity, TSnapshot>
            where TIdentity : IIdentity
            where TSnapshot : ISnapshot
        {
            var snapsnotDefinition = _snapshotDefinitionService.GetDefinition(typeof(TSnapshot));

            _log.Verbose(() => $"Building snapshot '{snapsnotDefinition.Name}' v{snapsnotDefinition.Version} for {typeof(TAggregate).PrettyPrint()}");

            var updatedSnapshotMetadata = new SnapshotMetadata(snapshotContainer.Metadata.Concat(new Dictionary<string, string>
                {
                    {SnapshotMetadataKeys.SnapshotName, snapsnotDefinition.Name},
                    {SnapshotMetadataKeys.SnapshotVersion, snapsnotDefinition.Version.ToString()},
                }));

            var serializedMetadata = _jsonSerializer.Serialize(updatedSnapshotMetadata);
            var serializedData = _jsonSerializer.Serialize(snapshotContainer.Snapshot);

            return Task.FromResult(new SerializedSnapshot(
                serializedMetadata,
                serializedData,
                updatedSnapshotMetadata));
        }

        public async Task<SnapshotContainer> DeserializeAsync<TAggregate, TIdentity, TSnapshot>(
            CommittedSnapshot committedSnapshot,
            CancellationToken cancellationToken)
            where TAggregate : ISnapshotAggregateRoot<TIdentity, TSnapshot>
            where TIdentity : IIdentity
            where TSnapshot : ISnapshot
        {
            if (committedSnapshot == null) throw new ArgumentNullException(nameof(committedSnapshot));

            var metadata = _jsonSerializer.Deserialize<SnapshotMetadata>(committedSnapshot.SerializedMetadata);
            var snapshotDefinition = _snapshotDefinitionService.GetDefinition(metadata.SnapshotName, metadata.SnapshotVersion);

            _log.Verbose(() => $"Deserializing snapshot named '{snapshotDefinition.Name}' v{snapshotDefinition.Version} for '{typeof(TAggregate).PrettyPrint()}' v{metadata.AggregateSequenceNumber}");

            var snapshot = (ISnapshot)_jsonSerializer.Deserialize(committedSnapshot.SerializedData, snapshotDefinition.Type);
            var upgradedSnapshot = await _snapshotUpgradeService.UpgradeAsync(snapshot, cancellationToken).ConfigureAwait(false);

            return new SnapshotContainer(upgradedSnapshot, metadata);
        }
    }
}