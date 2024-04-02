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

using StackExchange.Redis;

namespace EventFlow.Redis.EventStore;

internal class EventStreamCollectionResolver : IEventStreamCollectionResolver
{
    private readonly IConnectionMultiplexer _multiplexer;

    public EventStreamCollectionResolver(IConnectionMultiplexer multiplexer)
    {
        _multiplexer = multiplexer;
    }

    // Using https://redis.io/commands/scan/ instead of KEYS to reduce blocking on the server.
    public async Task<IReadOnlyCollection<PrefixedKey>> GetStreamIdsAsync(CancellationToken cancellationToken = default)
    {
        var cursor = 0;
        var names = new List<PrefixedKey>();
        do
        {
            var result = await _multiplexer
                .GetDatabase()
                .ExecuteAsync("scan", cursor, "MATCH", $"{Constants.StreamPrefix}*")
                .ConfigureAwait(false);

            var arr = (RedisResult[]) result;
            cursor = (int) arr[0];
            var prefixedKeys = ((RedisResult[]) arr[1]).Select(n => AsPrefixedKey((string) n));
            names.AddRange(prefixedKeys);
        } while (cursor != 0);

        return names;
    }

    private static PrefixedKey AsPrefixedKey(string k)
    {
        var split = k.Split(':');
        return new PrefixedKey(split[0], split[1]);
    }
}