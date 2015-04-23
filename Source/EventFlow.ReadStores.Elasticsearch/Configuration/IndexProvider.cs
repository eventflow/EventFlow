using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nest;

namespace EventFlow.ReadStores.Elasticsearch.Configuration
{
    public class IndexProvider : IIndexProvider
    {
        private readonly IIndexIdentifier[] _indexIdentifiers;
        private readonly IElasticClient _elasticClient;

        public IndexProvider(IIndexIdentifier[] indexIdentifiers, IElasticClient elasticClient)
        {
            _indexIdentifiers = indexIdentifiers;
            _elasticClient = elasticClient;
        }

        public async Task<IEnumerable<string>> GetIndiciesAsync()
        {
            var indicesTasks = _indexIdentifiers.Select(x => x.GetIndicesToFeedAsync(_elasticClient)).ToList();
            var indiciesToFeed = (await(Task.WhenAll(indicesTasks)).ConfigureAwait(false)).SelectMany(x => x);

            return indiciesToFeed;
        }
    }
}
