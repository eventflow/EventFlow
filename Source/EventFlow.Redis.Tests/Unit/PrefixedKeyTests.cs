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

using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;
using StackExchange.Redis;

namespace EventFlow.Redis.Tests.Unit;

[Category(Categories.Unit)]
public class PrefixedKeyTests
{
    [Test]
    public void EmptyPrefixCantExist()
    {
        //Arrange
        var prefix = string.Empty;
        var key = "KEY";

        //Act
        Action act = () => new PrefixedKey(prefix, key);

        //Assert
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void EmptyKeyCantExist()
    {
        //Arrange
        var prefix = "PREFIX";
        var key = string.Empty;

        //Act
        Action act = () => new PrefixedKey(prefix, key);

        //Assert
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void KeyContainsCorrectValues()
    {
        //Arrange
        var prefix = "PREFIX";
        var key = "KEY";

        //Act
        var prefixedKey = new PrefixedKey(prefix, key);

        //Assert
        prefixedKey.Prefix.Should().Be(prefix);
        prefixedKey.Key.Should().Be(key);
    }

    [Test]
    public void KeyCanBeRedisKey()
    {
        //Arrange
        var prefix = "PREFIX";
        var key = "KEY";
        var prefixedKey = new PrefixedKey(prefix, key);

        //Act
        var act = () => (RedisKey) prefixedKey;

        //Assert
        act.Should().NotThrow();
    }
}