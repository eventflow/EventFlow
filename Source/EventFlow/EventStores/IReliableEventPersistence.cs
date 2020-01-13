using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;

namespace EventFlow.EventStores
{
    public interface IReliableEventPersistence: IEventPersistence
    {
        Task ConfirmEventsAsync(
            IIdentity id,
            IReadOnlyCollection<IMetadata> eventsMetadata,
            CancellationToken cancellationToken);

        Task<AllCommittedEventsPage> LoadAllUnconfirmedEvents(
            GlobalPosition globalPosition,
            int pageSize,
            CancellationToken cancellationToken);

        Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadUnconfirmedEventsAsync(
            IIdentity id,
            int fromEventSequenceNumber,
            CancellationToken cancellationToken);
    }
}