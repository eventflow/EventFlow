using EventFlow.ValueObjects;
using MongoDB.Bson.Serialization.Attributes;

namespace EventFlow.MongoDB.ValueObjects
{
    public class MongoDbCounterDataModel : ValueObject
    {
        public string _id { get; set; }

        [BsonElement("seq")]
        public int Seq { get; set; }
    }
}
