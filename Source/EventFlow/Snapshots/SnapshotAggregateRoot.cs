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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.Snapshots.Strategies;

namespace EventFlow.Snapshots
{
    public abstract class SnapshotAggregateRoot<TAggregate, TIdentity, TSnapshot> : AggregateRoot<TAggregate, TIdentity>,
        ISnapshotAggregateRoot<TIdentity, TSnapshot>
        where TAggregate : SnapshotAggregateRoot<TAggregate, TIdentity, TSnapshot>
        where TIdentity : IIdentity
        where TSnapshot : ISnapshot
    {
        protected ISnapshotStrategy SnapshotStrategy { get; }

        protected SnapshotAggregateRoot(
            TIdentity id,
            ISnapshotStrategy snapshotStrategy)
            : base(id)
        {
            SnapshotStrategy = snapshotStrategy;
        }

        public int? SnapshotVersion { get; private set; }

        public override async Task LoadAsync(
            IEventStore eventStore,
            ISnapshotStore snapshotStore,
            CancellationToken cancellationToken)
        {
            var snapshot = await snapshotStore.LoadSnapshotAsync<TAggregate, TIdentity, TSnapshot>(
                Id,
                cancellationToken)
                .ConfigureAwait(false);
            if (snapshot == null)
            {
                await base.LoadAsync(eventStore, snapshotStore, cancellationToken).ConfigureAwait(false);
                return;
            }

            await LoadSnapshotContainerAsync(snapshot, cancellationToken).ConfigureAwait(false);

            Version = snapshot.Metadata.AggregateSequenceNumber;
            AddPreviousSourceIds(snapshot.Metadata.PreviousSourceIds);
            var domainEvents = await eventStore.LoadEventsAsync<TAggregate, TIdentity>(
                Id,
                Version + 1,
                cancellationToken)
                .ConfigureAwait(false);

            ApplyEvents(domainEvents);
        }

        public override async Task<IReadOnlyCollection<IDomainEvent>> CommitAsync(
            IEventStore eventStore,
            ISnapshotStore snapshotStore,
            ISourceId sourceId,
            CancellationToken cancellationToken)
        {
            var domainEvents = await base.CommitAsync(eventStore, snapshotStore, sourceId, cancellationToken).ConfigureAwait(false);

            if (!await SnapshotStrategy.ShouldCreateSnapshotAsync(this, cancellationToken).ConfigureAwait(false))
            {
                return domainEvents;
            }

            var snapshotContainer = await CreateSnapshotContainerAsync(sourceId, cancellationToken).ConfigureAwait(false);
            await snapshotStore.StoreSnapshotAsync<TAggregate, TIdentity, TSnapshot>(
                Id,
                snapshotContainer,
                cancellationToken)
                .ConfigureAwait(false);

            return domainEvents;
        }

        private async Task<SnapshotContainer> CreateSnapshotContainerAsync(
            ISourceId sourceId,
            CancellationToken cancellationToken)
        {
            var snapshotTask = CreateSnapshotAsync(cancellationToken);
            var snapshotMetadataTask = CreateSnapshotMetadataAsync(sourceId, cancellationToken);

            await Task.WhenAll(snapshotTask, snapshotMetadataTask).ConfigureAwait(false);

            var snapshotContainer = new SnapshotContainer(
                snapshotTask.Result,
                snapshotMetadataTask.Result);

            return snapshotContainer;
        }

        private Task LoadSnapshotContainerAsync(SnapshotContainer snapshotContainer, CancellationToken cancellationToken)
        {
            if (SnapshotVersion.HasValue)
            {
                throw new InvalidOperationException($"Aggregate '{Id}' of type '{GetType().PrettyPrint()}' already has snapshot loaded");
            }

            if (Version > 0)
            {
                throw new InvalidOperationException($"Aggregate '{Id}' of type '{GetType().PrettyPrint()}' already has events loaded");
            }

            if (!(snapshotContainer.Snapshot is TSnapshot snapshot))
            {
                throw new ArgumentException($"Snapshot '{snapshotContainer.Snapshot.GetType().PrettyPrint()}' for aggregate '{GetType().PrettyPrint()}' is not of type '{typeof(TSnapshot).PrettyPrint()}'. Did you forget to implement a snapshot upgrader?");
            }

            SnapshotVersion = snapshotContainer.Metadata.AggregateSequenceNumber;

            AddPreviousSourceIds(snapshotContainer.Metadata.PreviousSourceIds);

            return LoadSnapshotAsync(
                snapshot,
                snapshotContainer.Metadata,
                cancellationToken);
        }

        protected abstract Task<TSnapshot> CreateSnapshotAsync(CancellationToken cancellationToken);

        protected abstract Task LoadSnapshotAsync(TSnapshot snapshot, ISnapshotMetadata metadata, CancellationToken cancellationToken);

        protected virtual Task<ISnapshotMetadata> CreateSnapshotMetadataAsync(
            ISourceId sourceId,
            CancellationToken cancellationToken)
        {
            // We need to append the current source ID that triggered the snapshot
            // as this hasn't been loaded via the event stream
            var sourceIds = PreviousSourceIds
                .Append(sourceId)
                .ToArray();

            var snapshotMetadata = (ISnapshotMetadata) new SnapshotMetadata
                {
                    AggregateId = Id.Value,
                    AggregateName = Name.Value,
                    AggregateSequenceNumber = Version,
                    PreviousSourceIds = sourceIds
                };

            return Task.FromResult(snapshotMetadata);
        }
    }
}