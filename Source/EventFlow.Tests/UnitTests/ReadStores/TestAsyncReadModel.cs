using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.ReadStores;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;

namespace EventFlow.Tests.UnitTests.ReadStores
{
    public class TestAsyncReadModel : IReadModel, IAmAsyncReadModelFor<ThingyAggregate, ThingyId, ThingyPingEvent>
    {
        public Task ApplyAsync( 
            IReadModelContext context, 
            IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent> domainEvent, 
            CancellationToken cancellationToken )
        {
            return Task.Delay( 0, cancellationToken );
        }
    }
}