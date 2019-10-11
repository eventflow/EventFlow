using EventFlow.ReadStores;

namespace EventFlow.EventStores.StreamsDb
{
    public interface IStreamsDbReadModelStore<TReadModel> : IReadModelStore<TReadModel>
        where TReadModel : class, IReadModel
    {
    }
}
