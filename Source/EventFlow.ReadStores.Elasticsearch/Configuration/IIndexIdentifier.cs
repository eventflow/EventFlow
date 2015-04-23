using System.Collections.Generic;
using System.Threading.Tasks;
using Nest;

namespace EventFlow.ReadStores.Elasticsearch.Configuration
{
    public interface IIndexIdentifier
    {
        Task<IEnumerable<string>> GetIndicesToFeedAsync(IElasticClient elasticClient);
    }
}