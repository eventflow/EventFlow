﻿// The MIT License (MIT)
// 
// Copyright (c) 2015-2019 Rasmus Mikkelsen
// Copyright (c) 2015-2019 eBay Software Foundation
// https://github.com/eventflow/EventFlow
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections;
using EventFlow.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace EventFlow.MongoDB.ValueObjects
{
    public class MongoDbSnapshotDataModel : MongoDbSnapshotDataModel<string>
    {
    }

    public class MongoDbSnapshotDataModel<TSerialized> : ValueObject
        where TSerialized : IEnumerable
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
        public TSerialized Data { get; set; }
        [JsonProperty("metaData")]
        public TSerialized Metadata { get; set; }
    }
}
