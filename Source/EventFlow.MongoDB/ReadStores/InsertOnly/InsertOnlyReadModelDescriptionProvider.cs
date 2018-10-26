using System;
using System.Collections.Concurrent;
using System.Reflection;
using EventFlow.Extensions;
using EventFlow.MongoDB.ReadStores.Attributes;
using EventFlow.MongoDB.ValueObjects;

namespace EventFlow.MongoDB.ReadStores.InsertOnly
{
    public class InsertOnlyReadModelDescriptionProvider : IInsertOnlyReadModelDescriptionProvider
    {
        private static readonly ConcurrentDictionary<Type, ReadModelDescription> CollectionNames
            = new ConcurrentDictionary<Type, ReadModelDescription>();

        public ReadModelDescription GetReadModelDescription<TReadModel>() where TReadModel : IMongoDbInsertOnlyReadModel
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
