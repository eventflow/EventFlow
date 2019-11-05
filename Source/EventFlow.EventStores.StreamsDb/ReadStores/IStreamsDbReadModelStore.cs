using System.Threading;
using System.Threading.Tasks;
using EventFlow.ReadStores;

namespace EventFlow.EventStores.StreamsDb
{
    public interface IStreamsDbReadModelStore<TReadModel> : IReadModelStore<TReadModel>
        where TReadModel : class, IReadModel
    {
    }
}
