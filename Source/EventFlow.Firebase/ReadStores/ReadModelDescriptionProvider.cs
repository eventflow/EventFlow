using System;
using EventFlow.Firebase.ValueObjects;
using EventFlow.ReadStores;
using System.Collections.Concurrent;
using EventFlow.Extensions;

namespace EventFlow.Firebase.ReadStores
{
    public class ReadModelDescriptionProvider : IReadModelDescriptionProvider
    {
        private static readonly ConcurrentDictionary<Type, ReadModelDescription> NodeNames 
            = new ConcurrentDictionary<Type, ReadModelDescription>();

        public ReadModelDescription GetReadModelDescription<TReadModel>() 
            where TReadModel : IReadModel
        {
            return NodeNames.GetOrAdd(typeof(TReadModel), t => 
            {
                var nodeName = $"eventflow-{typeof(TReadModel).PrettyPrint().ToLowerInvariant()}";
                return new ReadModelDescription(new NodeName(nodeName));
            });

            /*
             return IndexNames.GetOrAdd(
                typeof (TReadModel),
                t =>
                    {
                        var elasticType = t.GetCustomAttribute<ElasticsearchTypeAttribute>();
                        var indexName = elasticType == null
                            ? $"eventflow-{typeof(TReadModel).PrettyPrint().ToLowerInvariant()}"
                            : elasticType.Name;
                        return new ReadModelDescription(new IndexName(indexName));
                    });
             */
        }
    }
}
