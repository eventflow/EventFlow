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
using static LinqToDB.LinqExtensions;
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
            var endPosition = startPosition + pageSize;

            using (var context = _contextProvider.CreateContext())
            {
                var entities = await context
                    .Set<EventEntity>()
                    .Where(e => e.GlobalSequenceNumber >= startPosition
                                && e.GlobalSequenceNumber <= endPosition)
                    .OrderBy(e => e.GlobalSequenceNumber)
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
                    context.AddRange(entities);
                    await context.SaveChangesAsync(cancellationToken)
                        .ConfigureAwait(false);
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
                var affectedRows = await context.Set<EventEntity>()
                    .Where(e => e.AggregateId == id.Value)
                    .DeleteAsync(cancellationToken)
                    .ConfigureAwait(false);

                _log.Verbose(
                    "Deleted entity with ID '{0}' by deleting all of its {1} events",
                    id,
                    affectedRows);
            }
        }
    }
}
