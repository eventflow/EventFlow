using EventFlow.Redis.ReadStore;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Redis.Tests.Unit;

[Category(Categories.Unit)]
public class HashBuilderTests
{
    [Test]
    public void ReturnsEmptyDictionaryForPlainObject()
    {
        //Arrange
        var obj = new object();
        var builder = new RedisHashBuilder();

        //Act
        var dict = builder.BuildHashSet(obj);

        //Assert
        dict.Should().BeEmpty();
    }

    [Test]
    public void ReturnsDictionaryWithDefaultEntriesForEmptyObject()
    {
        //Arrange
        var obj = new HashTestClass();
        var valueName = nameof(obj.Value);
        var builder = new RedisHashBuilder();

        //Act
        var dict = builder.BuildHashSet(obj);

        //Assert
        dict.Should().NotBeEmpty();
        dict.Keys.Should().Contain(valueName);
        dict[valueName].Should().Be("0");
    }

    [Test]
    public void ReturnsDictionaryWithValidValues()
    {
        //Arrange
        var obj = new HashTestClass();
        obj.Value = 10;
        var valueName = nameof(obj.Value);
        var builder = new RedisHashBuilder();

        //Act
        var dict = builder.BuildHashSet(obj);

        //Assert
        dict.Should().NotBeEmpty();
        dict.Keys.Should().Contain(valueName);
        dict[valueName].Should().Be(obj.Value.ToString());
    }

    [Test]
    public void ReturnsEmptyDictionaryForNull()
    {
        //Arrange
        var builder = new RedisHashBuilder();

        //Act
        var dict = builder.BuildHashSet(null);

        //Assert
        dict.Should().BeEmpty();
    }
}

public class HashTestClass
{
    public int Value { get; set; }
}