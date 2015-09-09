// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
// https://github.com/rasmus/EventFlow
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

using System;
using System.Collections.Generic;
using System.Linq;
using EventFlow.Core;
using EventFlow.Exceptions;
using EventFlow.Extensions;
using Newtonsoft.Json;

namespace EventFlow.Aggregates
{
    public class Metadata : Dictionary<string, string>, IMetadata
    {
        public static IMetadata Empty { get; } = new Metadata();

        public static IMetadata With(IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            return new Metadata(keyValuePairs);
        }

        public static IMetadata With(params KeyValuePair<string, string>[] keyValuePairs)
        {
            return new Metadata(keyValuePairs);
        }

        public static IMetadata With(IDictionary<string, string> keyValuePairs)
        {
            return new Metadata(keyValuePairs);
        }

        public SourceId SourceId
        {
            get { return GetMetadataValue(MetadataKeys.SourceId, v => new SourceId(v)); }
            set { Add(MetadataKeys.SourceId, value.Value); }
        }

        [JsonIgnore]
        public string EventName
        {
            get { return GetMetadataValue(MetadataKeys.EventName); }
            set { Add(MetadataKeys.EventName, value); }
        }

        [JsonIgnore]
        public int EventVersion
        {
            get { return GetMetadataValue(MetadataKeys.EventVersion, int.Parse); }
            set { Add(MetadataKeys.EventVersion, value.ToString()); }
        }

        [JsonIgnore]
        public DateTimeOffset Timestamp
        {
            get { return GetMetadataValue(MetadataKeys.Timestamp, DateTimeOffset.Parse); }
            set { Add(MetadataKeys.Timestamp, value.ToString("O")); }
        }

        [JsonIgnore]
        public long TimestampEpoch
        {
            get
            {
                string timestampEpoch;
                return TryGetValue(MetadataKeys.TimestampEpoch, out timestampEpoch)
                    ? long.Parse(timestampEpoch)
                    : Timestamp.ToUnixTime();
            }
        }

        [JsonIgnore]
        public int AggregateSequenceNumber
        {
            get { return GetMetadataValue(MetadataKeys.AggregateSequenceNumber, int.Parse); }
            set { Add(MetadataKeys.AggregateSequenceNumber, value.ToString()); }
        }

        [JsonIgnore]
        public string AggregateId
        {
            get { return GetMetadataValue(MetadataKeys.AggregateId); }
            set { Add(MetadataKeys.AggregateId, value); }
        }

        [JsonIgnore]
        public IEventId EventId
        {
            get { return GetMetadataValue(MetadataKeys.EventId, Aggregates.EventId.With); }
            set { Add(MetadataKeys.EventId, value.Value); }
        }

        [JsonIgnore]
        public string AggregateName
        {
            get { return GetMetadataValue(MetadataKeys.AggregateName); }
            set { Add(MetadataKeys.AggregateName, value); }
        }

        public Metadata() { }

        public Metadata(IDictionary<string, string> keyValuePairs)
            : base(keyValuePairs)
        {
        }

        public Metadata(IEnumerable<KeyValuePair<string, string>> keyValuePairs)
            : base(keyValuePairs.ToDictionary(kv => kv.Key, kv => kv.Value))
        {
        }

        public Metadata(params KeyValuePair<string, string>[] keyValuePairs)
            : this((IEnumerable<KeyValuePair<string, string>>) keyValuePairs)
        {
        }

        public IMetadata CloneWith(params KeyValuePair<string, string>[] keyValuePairs)
        {
            return CloneWith((IEnumerable<KeyValuePair<string, string>>) keyValuePairs);
        }

        public IMetadata CloneWith(IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            var metadata = new Metadata(this);
            foreach (var kv in keyValuePairs)
            {
                if (metadata.ContainsKey(kv.Key))
                {
                    throw new ArgumentException($"Key '{kv.Key}' is already present!");
                }
                metadata[kv.Key] = kv.Value;
            }
            return metadata;
        }

        public void AddRange(params KeyValuePair<string, string>[] keyValuePairs)
        {
            AddRange((IEnumerable<KeyValuePair<string, string>>) keyValuePairs);
        }

        public void AddRange(IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            foreach (var keyValuePair in keyValuePairs)
            {
                Add(keyValuePair.Key, keyValuePair.Value);
            }
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, this.Select(kv => $"{kv.Key}: {kv.Value}"));
        }

        public string GetMetadataValue(string key)
        {
            return GetMetadataValue(key, s => s);
        }

        public T GetMetadataValue<T>(string key, Func<string, T> converter)
        {
            string value;

            if (!TryGetValue(key, out value))
            {
                throw new MetadataKeyNotFoundException(key);
            }

            try
            {
                return converter(value);
            }
            catch (Exception e)
            {
                throw new MetadataParseException(key, value, e);
            }
        }
    }
}
