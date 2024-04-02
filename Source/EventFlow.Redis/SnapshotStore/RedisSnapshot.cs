// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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

using Redis.OM.Modeling;

namespace EventFlow.Redis.SnapshotStore;

//Storage type json is required due to https://github.com/redis/redis-om-dotnet/issues/175
[Document(StorageType = StorageType.Json, Prefixes = new[] {Constants.SnapshotPrefix})]
public class RedisSnapshot
{
    [RedisIdField] public string Id { get; set; }
    public long? Version { get; set; }
    [Indexed] public string AggregateId { get; set; }
    [Indexed] public string AggregateName { get; set; }
    [Indexed] public int AggregateSequenceNumber { get; set; }
    public string Data { get; set; }
    public string Metadata { get; set; }
}