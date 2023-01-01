using System;

namespace EventFlow.Elasticsearch.ReadStores.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ElasticsearchIndexAttribute : Attribute
    {
        public ElasticsearchIndexAttribute(string indexName)
        {
            IndexName = indexName.ToLowerInvariant();
        }

        public string IndexName { get; set; }
    }
}