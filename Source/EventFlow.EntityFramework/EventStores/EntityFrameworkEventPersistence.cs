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
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EntityFramework.Extensions;
using EventFlow.EventStores;
using EventFlow.Exceptions;
using EventFlow.Logs;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework.EventStores
{
    public class EntityFrameworkEventPersistence<TDbContext> : IEventPersistence
        where TDbContext : DbContext
    {
        private readonly IDbContextProvider<TDbContext> _contextProvider;
        private readonly ILog _log;
        private readonly IUniqueConstraintDetectionStrategy _strategy;

        public EntityFrameworkEventPersistence(
            ILog log,
            IDbContextProvider<TDbContext> contextProvider,
            IUniqueConstraintDetectionStrategy strategy
        )
        {
            _log = log;
            _contextProvider = contextProvider;
            _strategy = strategy;
        }

        public async Task<AllCommittedEventsPage> LoadAllCommittedEvents(GlobalPosition globalPosition, int pageSize,
            CancellationToken cancellationToken)
        {
            var startPosition = globalPosition.IsStart
                ? 0
                : long.Parse(globalPosition.Value);

            using (var context = _contextProvider.CreateContext())
            {
                var entities = await context
                    .Set<EventEntity>()
                    .OrderBy(e => e.GlobalSequenceNumber)
                    .Where(e => e.GlobalSequenceNumber >= startPosition)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                var nextPosition = entities.Any()
                    ? entities.Max(e => e.GlobalSequenceNumber) + 1
                    : startPosition;

                return new AllCommittedEventsPage(new GlobalPosition(nextPosition.ToString()), entities);
            }
        }

        public async Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync(IIdentity id,
            IReadOnlyCollection<SerializedEvent> serializedEvents, CancellationToken cancellationToken)
        {
            if (!serializedEvents.Any())
                return new ICommittedDomainEvent[0];

            var entities = serializedEvents
                .Select((e, i) => new EventEntity
                {
                    AggregateId = id.Value,
                    AggregateName = e.Metadata[MetadataKeys.AggregateName],
                    BatchId = Guid.Parse(e.Metadata[MetadataKeys.BatchId]),
                    Data = e.SerializedData,
                    Metadata = e.SerializedMetadata,
                    AggregateSequenceNumber = e.AggregateSequenceNumber
                })
                .ToList();

            _log.Verbose(
                "Committing {0} events to EntityFramework event store for entity with ID '{1}'",
                entities.Count,
                id);

            try
            {
                using (var context = _contextProvider.CreateContext())
                {
                    foreach (EventEntity entity in entities)
                    {
                        context.Add(entity);
                        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation(_strategy))
            {
                _log.Verbose(
                    "Entity Framework event insert detected an optimistic concurrency " +
                    "exception for entity with ID '{0}'", id);
                throw new OptimisticConcurrencyException(ex.Message, ex);
            }

            return entities;
        }

        public async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(IIdentity id,
            int fromEventSequenceNumber, CancellationToken cancellationToken)
        {
            using (var context = _contextProvider.CreateContext())
            {
                var entities = await context
                    .Set<EventEntity>()
                    .Where(e => e.AggregateId == id.Value
                                && e.AggregateSequenceNumber >= fromEventSequenceNumber)
                    .OrderBy(e => e.AggregateSequenceNumber)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                return entities;
            }
        }

        public async Task DeleteEventsAsync(IIdentity id, CancellationToken cancellationToken)
        {
            using (var context = _contextProvider.CreateContext())
            {
                var entities = await context.Set<EventEntity>()
                    .Where(e => e.AggregateId == id.Value)
                    .Select(e => new EventEntity {GlobalSequenceNumber = e.GlobalSequenceNumber})
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                context.RemoveRange(entities);
                var rowsAffected = await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                _log.Verbose(
                    "Deleted entity with ID '{0}' by deleting all of its {1} events",
                    id,
                    rowsAffected);
            }
        }
    }
}