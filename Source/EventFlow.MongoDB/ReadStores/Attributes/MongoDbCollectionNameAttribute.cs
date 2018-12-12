using System;

namespace EventFlow.MongoDB.ReadStores.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MongoDbCollectionNameAttribute : Attribute
    {
        public MongoDbCollectionNameAttribute(string collectionName)
        {
            this.CollectionName = collectionName;
        }

        public virtual string CollectionName { get; }
    }
}
