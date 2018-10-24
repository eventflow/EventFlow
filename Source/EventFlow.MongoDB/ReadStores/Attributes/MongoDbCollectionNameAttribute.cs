using System;

namespace EventFlow.MongoDB.ReadStores.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MongoDbCollectionNameAttribute : Attribute
    {
        private string collectionName;
        public MongoDbCollectionNameAttribute(string collectionName)
        {
            this.collectionName = collectionName;
        }

        public virtual string CollectionName
        {
            get { return collectionName; }
        }

    }
}
