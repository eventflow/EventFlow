// The MIT License (MIT)
// 
// Copyright (c) 2015-2020 Rasmus Mikkelsen
// Copyright (c) 2015-2020 eBay Software Foundation
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

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EventFlow.Core;
using Newtonsoft.Json;

namespace EventFlow.Snapshots
{
    public class SnapshotMetadata : MetadataContainer, ISnapshotMetadata
    {
        public SnapshotMetadata()
        {
        }

        public SnapshotMetadata(IDictionary<string, string> keyValuePairs)
            : base(keyValuePairs)
        {
        }

        public SnapshotMetadata(IEnumerable<KeyValuePair<string, string>> keyValuePairs)
            : base(keyValuePairs.ToDictionary(kv => kv.Key, kv => kv.Value))
        {
        }

        public SnapshotMetadata(params KeyValuePair<string, string>[] keyValuePairs)
            : this((IEnumerable<KeyValuePair<string, string>>) keyValuePairs)
        {
        }

        [JsonIgnore]
        public string AggregateId
        {
            get { return GetMetadataValue(SnapshotMetadataKeys.AggregateId); }
            set { Add(SnapshotMetadataKeys.AggregateId, value); }
        }

        [JsonIgnore]
        public string AggregateName
        {
            get { return GetMetadataValue(SnapshotMetadataKeys.AggregateName); }
            set { Add(SnapshotMetadataKeys.AggregateName, value); }
        }

        [JsonIgnore]
        public int AggregateSequenceNumber
        {
            get { return GetMetadataValue(SnapshotMetadataKeys.AggregateSequenceNumber, int.Parse); }
            set { Add(SnapshotMetadataKeys.AggregateSequenceNumber, value.ToString(CultureInfo.InvariantCulture)); }
        }

        [JsonIgnore]
        public string SnapshotName
        {
            get { return GetMetadataValue(SnapshotMetadataKeys.SnapshotName); }
            set { Add(SnapshotMetadataKeys.SnapshotName, value); }
        }

        [JsonIgnore]
        public int SnapshotVersion
        {
            get { return GetMetadataValue(SnapshotMetadataKeys.SnapshotVersion, int.Parse); }
            set { Add(SnapshotMetadataKeys.SnapshotVersion, value.ToString(CultureInfo.InvariantCulture)); }
        }
    }
}