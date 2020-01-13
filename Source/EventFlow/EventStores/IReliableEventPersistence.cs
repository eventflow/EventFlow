using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;

namespace EventFlow.EventStores
{
    public interface IReliableEventPersistence: IEventPersistence
    {
        Task MarkEventsDeliveredAsync(
            IIdentity id,
            IReadOnlyCollection<IMetadata> eventMetadata,
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