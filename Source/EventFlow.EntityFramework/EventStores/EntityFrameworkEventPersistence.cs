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
    public class EntityFrameworkEventPersistence : IEventPersistence
    {
        private readonly ILog _log;
        private readonly IDbContextProvider _contextProvider;
        private readonly IUniqueConstraintViolationDetector _uniqueConstraintViolationDetector;

        public EntityFrameworkEventPersistence(
            ILog log, 
            IDbContextProvider<IEventPersistence> contextProvider, 
            IUniqueConstraintViolationDetector uniqueConstraintViolationDetector
        )
        {
            _log = log;
            _contextProvider = contextProvider;
            _uniqueConstraintViolationDetector = uniqueConstraintViolationDetector;
        }

        public async Task<AllCommittedEventsPage> LoadAllCommittedEvents(GlobalPosition globalPosition, int pageSize, CancellationToken cancellationToken)
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
                    .ToListAsync(cancellationToken);

                var nextPosition = entities.Any()
                    ? entities.Max(e => e.GlobalSequenceNumber) + 1
                    : startPosition;

                return new AllCommittedEventsPage(new GlobalPosition(nextPosition.ToString()), entities);
            }
        }

        public async Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync(IIdentity id, IReadOnlyCollection<SerializedEvent> serializedEvents, CancellationToken cancellationToken)
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
                    AggregateSequenceNumber = e.AggregateSequenceNumber,
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
                    await context.SaveChangesAsync(cancellationToken);
                }
            }
            catch (DbUpdateException exception)
            {
                if (_uniqueConstraintViolationDetector.IsUniqueContraintException(exception))
                {
                    _log.Verbose(
                        "Entity Framework event insert detected an optimistic concurrency " +
                        "exception for entity with ID '{0}'", id);
                    throw new OptimisticConcurrencyException(exception.Message, exception);
                }

                throw;
            }

            return entities;
        }

        public async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(IIdentity id, int fromEventSequenceNumber, CancellationToken cancellationToken)
        {
            using (var context = _contextProvider.CreateContext())
            {
                var entities = await context
                    .Set<EventEntity>()
                    .Where(e => e.AggregateId == id.Value
                                && e.AggregateSequenceNumber >= fromEventSequenceNumber)
                    .OrderBy(e => e.AggregateSequenceNumber)
                    .ToListAsync(cancellationToken);

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
                    .ToListAsync(cancellationToken);

                context.RemoveRange(entities);
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}