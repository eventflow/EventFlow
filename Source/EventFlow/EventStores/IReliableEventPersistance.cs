using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;

namespace EventFlow.EventStores
{
    public interface IReliableEventPersistance: IEventPersistence
    {
        Task MarkEventsDeliveredAsync(
            IIdentity id,
            IReadOnlyCollection<IEventId> eventIds,
            CancellationToken cancellationToken);

        Task<AllCommittedEventsPage> LoadAllUndeliveredEvents(
            GlobalPosition globalPosition,
            int pageSize,
            CancellationToken cancellationToken);

        Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadUndeliveredEventsAsync(
            IIdentity id,
            int fromEventSequenceNumber,
            CancellationToken cancellationToken);
    }
}