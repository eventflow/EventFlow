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
using EventFlow.Extensions;

namespace EventFlow.Aggregates
{
    public class Metadata : Dictionary<string, string>, IMetadata
    {
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

        public string EventName
        {
            get { return this[MetadataKeys.EventName]; }
            set { this[MetadataKeys.EventName] = value; }
        }

        public int EventVersion
        {
            get { return int.Parse(this[MetadataKeys.EventVersion]); }
            set { this[MetadataKeys.EventVersion] = value.ToString(); }
        }

        public DateTimeOffset Timestamp
        {
            get { return DateTimeOffset.Parse(this[MetadataKeys.Timestamp]); }
            set { this[MetadataKeys.Timestamp] = value.ToString("O"); }
        }

        public long TimestampEpoch
        {
            get
            {
                string timestampEpoch;
                return TryGetValue(MetadataKeys.TimestampEpoch, out timestampEpoch)
                    ? long.Parse(this[MetadataKeys.TimestampEpoch])
                    : Timestamp.ToUnixTime();
            }
        }

        public int AggregateSequenceNumber
        {
            get { return int.Parse(this[MetadataKeys.AggregateSequenceNumber]); }
            set { this[MetadataKeys.AggregateSequenceNumber] = value.ToString(); }
        }

        public string AggregateId
        {
            get { return this[MetadataKeys.AggregateId]; }
            set { this[MetadataKeys.AggregateId] = value; }
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

        public IMetadata CloneWith(IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            var metadata = new Metadata(this);
            foreach (var kv in keyValuePairs)
            {
                if (metadata.ContainsKey(kv.Key))
                {
                    throw new ArgumentException(string.Format("Key '{0}' is already present!", kv.Key));
                }
                metadata[kv.Key] = kv.Value;
            }
            return metadata;
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, this.Select(kv => string.Format("{0}: {1}", kv.Key, kv.Value)));
        }
    }
}
