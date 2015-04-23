using System.Collections.Generic;
using System.Threading.Tasks;
using Nest;

namespace EventFlow.ReadStores.Elasticsearch.Configuration
{
    public class SimpleIndexIdentifier : IIndexIdentifier
    {
        private readonly IEnumerable<string> _indiciesToReffed;

        public SimpleIndexIdentifier(IEnumerable<string> indiciesToReffed)
        {
            _indiciesToReffed = indiciesToReffed;
        }

        public Task<IEnumerable<string>> GetIndicesToFeedAsync(IElasticClient elasticClient)
        {
            return Task.FromResult(_indiciesToReffed);
        }
    }
}