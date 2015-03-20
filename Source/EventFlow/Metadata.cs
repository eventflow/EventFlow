// The MIT License (MIT)
//
// Copyright (c) 2015 EventFlow
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

namespace EventFlow
{
    public sealed class MetadataKeys
    {
        public const string EventName = "event_name";
        public const string EventVersion = "event_version";
        public const string Timestamp = "timestamp";
        public const string GlobalSequenceNumber = "global_sequence_number";
    }

    public interface IMetadata : IReadOnlyDictionary<string, string>
    {
        IMetadata CloneWith(IEnumerable<KeyValuePair<string, string>> keyValuePairs);
    }

    public class Metadata : Dictionary<string, string>, IMetadata
    {
        public Metadata() { }

        public Metadata(IDictionary<string, string> dictionary)
            : base(dictionary)
        {
        }

        public Metadata(IEnumerable<KeyValuePair<string, string>> keyValuePairs)
            : base(keyValuePairs.ToDictionary(kv => kv.Key, kv => kv.Value))
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
