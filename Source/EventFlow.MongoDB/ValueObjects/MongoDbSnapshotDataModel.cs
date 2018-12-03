using EventFlow.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace EventFlow.MongoDB.ValueObjects
{
    public class MongoDbSnapshotDataModel : ValueObject
    {
        [BsonElement("_id")]
        public ObjectId _id { get; set; }
        long? _version { get; set; }
        [JsonProperty("aggregateId")]
        public string AggregateId { get; set; }
        [JsonProperty("aggregateName")]
        public string AggregateName { get; set; }
        [JsonProperty("aggregateSequenceNumber")]
        public int AggregateSequenceNumber { get; set; }
        [JsonProperty("data")]
        public string Data { get; set; }
        [JsonProperty("metaData")]
        public string Metadata { get; set; }
    }
}
