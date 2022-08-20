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