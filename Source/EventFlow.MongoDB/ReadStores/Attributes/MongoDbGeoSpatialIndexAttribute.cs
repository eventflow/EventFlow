using System;

namespace EventFlow.MongoDB.ReadStores.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MongoDbGeoSpatialIndexAttribute : Attribute
    {

    }
}
