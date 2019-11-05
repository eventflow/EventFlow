using System.Threading;
using System.Threading.Tasks;
using EventFlow.ReadStores;

namespace EventFlow.EventStores.StreamsDb.ReadStores
{
    public static class ReadModelStoreExtensions
    {
        public static async Task<ReadModelEnvelope<TReadModel>> GetAsync<TReadModel>(this IReadModelStore<TReadModel> store, CancellationToken cancellationToken) where TReadModel: class, IReadModel
        {
            return await store.GetAsync("null", cancellationToken);
        }
    }
}
