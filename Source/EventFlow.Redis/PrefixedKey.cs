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

namespace EventFlow.Redis;

public record PrefixedKey
{
    public PrefixedKey(string prefix, string key)
    {
        if (string.IsNullOrEmpty(prefix))
            throw new ArgumentException("Prefix cant be empty");
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cant be empty");

        Prefix = prefix;
        Key = key;
    }

    public string Prefix { get; private set; }
    public string Key { get; private set; }

    public override string ToString() => $"{Prefix}:{Key}";

    public static implicit operator RedisKey(PrefixedKey prefixedKey) => new RedisKey(prefixedKey.ToString());
    public static implicit operator string(PrefixedKey prefixedKey) => prefixedKey.ToString();
}