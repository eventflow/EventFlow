using EventFlow.Extensions;
using EventFlow.MongoDB.ReadStores.Attributes;
using EventFlow.MongoDB.ValueObjects;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace EventFlow.MongoDB.ReadStores
{
    public class ReadModelDescriptionProvider : IReadModelDescriptionProvider
    {
        private static readonly ConcurrentDictionary<Type, ReadModelDescription> CollectionNames
            = new ConcurrentDictionary<Type, ReadModelDescription>();

        public ReadModelDescription GetReadModelDescription<TReadModel>() where TReadModel : IMongoDbReadModel
        {
            return CollectionNames.GetOrAdd(
                typeof(TReadModel),
                t =>
                {
                    var collectionType = t.GetTypeInfo().GetCustomAttribute<MongoDbCollectionNameAttribute>();
                    var indexName = collectionType == null
                        ? $"eventflow-{typeof(TReadModel).PrettyPrint().ToLowerInvariant()}"
                        : collectionType.CollectionName;
                    return new ReadModelDescription(new RootCollectionName(indexName));
                });

        }
    }
}
