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
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.Snapshots.Stores;
using EventFlow.Snapshots.Strategies;

namespace EventFlow.Snapshots
{
    public class SnapshotStore : ISnapshotStore
    {
        private readonly ILog _log;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ISnapshotBuilder _snapshotBuilder;
        private readonly ISnapshotDefinitionService _snapshotDefinitionService;
        private readonly ISnapshotUpgradeService _snapshotUpgradeService;
        private readonly ISnapshotPersistence _snapshotPersistence;
        private readonly IAggregateFactory _aggregateFactory;
        private readonly ISnapshotStrategy _snapshotStrategy;

        public SnapshotStore(
            ILog log,
            IJsonSerializer jsonSerializer,
            ISnapshotBuilder snapshotBuilder,
            ISnapshotDefinitionService snapshotDefinitionService,
            ISnapshotUpgradeService snapshotUpgradeService,
            ISnapshotPersistence snapshotPersistence,
            IAggregateFactory aggregateFactory,
            ISnapshotStrategy snapshotStrategy)
        {
            _log = log;
            _jsonSerializer = jsonSerializer;
            _snapshotBuilder = snapshotBuilder;
            _snapshotDefinitionService = snapshotDefinitionService;
            _snapshotUpgradeService = snapshotUpgradeService;
            _snapshotPersistence = snapshotPersistence;
            _aggregateFactory = aggregateFactory;
            _snapshotStrategy = snapshotStrategy;
        }

        public async Task<TAggregate> LoadSnapshotAsync<TAggregate, TIdentity>(
            TIdentity identity,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            if (!typeof (ISnapshotAggregateRoot).IsAssignableFrom(typeof (TAggregate)))
            {
                _log.Verbose(() => $"Aggregate '{typeof(TAggregate).PrettyPrint()}' is not a snapshot aggregate, so cannot load one");
                return default(TAggregate);
            }

            _log.Verbose(() => $"Fetching snapshot for '{typeof(TAggregate).PrettyPrint()}' with ID '{identity}'");
            var committedSnapshot = await _snapshotPersistence.GetSnapshotAsync(typeof (TAggregate), identity, cancellationToken).ConfigureAwait(false);
            if (committedSnapshot == null)
            {
                _log.Verbose(() => $"No snapshot found for '{typeof(TAggregate).PrettyPrint()}' with ID '{identity}'");
                return default(TAggregate);
            }

            var metadata = _jsonSerializer.Deserialize<SnapshotMetadata>(committedSnapshot.SerializedMetadata);
            var snapshotDefinition = _snapshotDefinitionService.GetDefinition(metadata.SnapshotName, metadata.SnapshotVersion);
            _log.Verbose(() => $"Found snapshot named '{snapshotDefinition.Name}' v{snapshotDefinition.Version} for '{typeof(TAggregate).PrettyPrint()}' with ID '{identity}' v{metadata.AggregateSequenceNumber}");

            var snapshot = (ISnapshot) _jsonSerializer.Deserialize(committedSnapshot.SerializedData, snapshotDefinition.Type);
            var upgradedSnapshot = await _snapshotUpgradeService.UpgradeAsync(snapshot, cancellationToken).ConfigureAwait(false);
            var aggregate = (ISnapshotAggregateRoot) await _aggregateFactory.CreateNewAggregateAsync<TAggregate, TIdentity>(identity);
            var snapshotContainer = new SnapshotContainer(upgradedSnapshot, metadata);

            await aggregate.LoadSnapshotAsyncAsync(snapshotContainer, cancellationToken).ConfigureAwait(false);

            return (TAggregate) aggregate;
        }

        public async Task StoreSnapshotAsync<TAggregate, TIdentity>(
            TAggregate aggregate,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            var snapshotAggregateRoot = aggregate as ISnapshotAggregateRoot;
            if (snapshotAggregateRoot == null)
            {
                _log.Verbose(() => $"Aggregate '{typeof(TAggregate).PrettyPrint()}' is not a snapshot aggregate");
                return;
            }

            if (!await _snapshotStrategy.ShouldCreateSnapshotAsync(snapshotAggregateRoot, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            var serializedSnapshot = await _snapshotBuilder.BuildSnapshotAsync(aggregate, cancellationToken).ConfigureAwait(false);

            await _snapshotPersistence.SetSnapshotAsync(typeof (TAggregate), aggregate.Id, serializedSnapshot, cancellationToken).ConfigureAwait(false);
        }
    }
}