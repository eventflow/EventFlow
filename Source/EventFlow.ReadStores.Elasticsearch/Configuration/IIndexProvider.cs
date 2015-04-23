using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventFlow.ReadStores.Elasticsearch.Configuration
{
    public interface IIndexProvider
    {
        Task<IEnumerable<string>> GetIndiciesAsync();
    }
}