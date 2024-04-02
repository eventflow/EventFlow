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

using EventFlow.Redis.ReadStore;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;
using Redis.OM.Modeling;

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
        var valueName = nameof(obj.Primitive);
        var builder = new RedisHashBuilder();

        //Act
        var dict = builder.BuildHashSet(obj);

        //Assert
        dict.Should().NotBeEmpty();
        dict.Keys.Should().Contain(valueName);
        dict[valueName].Should().Be(0.ToString());
    }

    [Test]
    public void CanBuildWithPrimitiveValues()
    {
        //Arrange
        var obj = new HashTestClass();
        obj.Primitive = 10;
        var valueName = nameof(obj.Primitive);
        var builder = new RedisHashBuilder();

        //Act
        var dict = builder.BuildHashSet(obj);

        //Assert
        dict.Should().NotBeEmpty();
        dict.Keys.Should().Contain(valueName);
        dict[valueName].Should().Be(obj.Primitive.ToString());
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

    [Test]
    public void UsesRedisFieldNameAsKey()
    {
        //Arrange
        var redisFieldName = "ValueName";
        var nameValue = "X";
        var builder = new RedisHashBuilder();
        var obj = new HashTestClass();
        obj.String = nameValue;

        //Act
        var dict = builder.BuildHashSet(obj);

        //Assert
        dict.Keys.Should().Contain(redisFieldName);
        dict[redisFieldName].Should().Be(nameValue);
    }

    [Test]
    public void CanBuildWithEnumValues()
    {
        //Arrange
        var builder = new RedisHashBuilder();
        var obj = new HashTestClass();
        var name = nameof(obj.Enum);
        obj.Enum = HashTestClass.HashTestEnum.SecondEntry;

        //Act
        var dict = builder.BuildHashSet(obj);

        //Assert
        dict.Keys.Should().Contain(name);
        dict[name].Should().Be(((int) HashTestClass.HashTestEnum.SecondEntry).ToString());
    }

    [Test]
    public void CanBuildWithDateTimeOffsetValues()
    {
        //Arrange
        var builder = new RedisHashBuilder();
        var obj = new HashTestClass();
        var name = nameof(obj.DateTimeOffset);
        var offset = DateTimeOffset.Now;
        obj.DateTimeOffset = offset;

        //Act
        var dict = builder.BuildHashSet(obj);

        //Assert
        dict.Keys.Should().Contain(name);
        dict[name].Should().Be(offset.ToString("O")); //ISO 8601
    }

    [Test]
    public void CanBuildWithDateTimeValues()
    {
        //Arrange
        var builder = new RedisHashBuilder();
        var obj = new HashTestClass();
        var name = nameof(obj.DateTime);
        var dateTime = DateTime.Now;
        obj.DateTime = dateTime;

        //Act
        var dict = builder.BuildHashSet(obj);

        //Assert
        dict.Keys.Should().Contain(name);
        dict[name].Should().Be(new DateTimeOffset(dateTime).ToUnixTimeMilliseconds().ToString());
    }

    [Test]
    public void CanBuildWithPrimitiveCollections()
    {
        //Arrange
        var firstValue = 1;
        var secondValue = 2;
        var builder = new RedisHashBuilder();
        var obj = new HashTestClass();
        var firstName = $"{nameof(obj.PrimitiveCollection)}[0]";
        var secondName = $"{nameof(obj.PrimitiveCollection)}[1]";
        obj.PrimitiveCollection = new List<int> {firstValue, secondValue};

        //Act
        var dict = builder.BuildHashSet(obj);

        //Assert
        dict.Keys.Should().Contain(firstName);
        dict.Keys.Should().Contain(secondName);
        dict[firstName].Should().Be(firstValue.ToString());
        dict[secondName].Should().Be(secondValue.ToString());
    }

    [Test]
    public void CanBuildWithComplexCollections()
    {
        //Arrange
        var firstValue = new HashTestClass {Primitive = 1};
        var secondValue = new HashTestClass {Primitive = 2};
        var builder = new RedisHashBuilder();
        var obj = new HashTestClass();
        var firstName = $"{nameof(obj.ComplexCollection)}[0].Primitive";
        var secondName = $"{nameof(obj.ComplexCollection)}[1].Primitive";
        obj.ComplexCollection = new List<HashTestClass> {firstValue, secondValue};

        //Act
        var dict = builder.BuildHashSet(obj);

        //Assert
        dict.Keys.Should().Contain(firstName);
        dict.Keys.Should().Contain(secondName);
        dict[firstName].Should().Be(firstValue.Primitive.ToString());
        dict[secondName].Should().Be(secondValue.Primitive.ToString());
    }

    [Test]
    public void CanBuildWithComplexObjects()
    {
        //Arrange
        var builder = new RedisHashBuilder();
        var obj = new HashTestClass();
        var str = "X";
        var complex = new HashTestClass
        {
            String = str
        };
        obj.Object = complex;
        var name = $"{nameof(obj.Object)}.ValueName";

        //Act
        var dict = builder.BuildHashSet(obj);

        //Assert
        dict.Keys.Should().Contain(name);
        dict[name].Should().Be(str);
    }
}

public class HashTestClass
{
    public enum HashTestEnum
    {
        FirstEntry,
        SecondEntry
    }

    public int Primitive { get; set; }

    [RedisField(PropertyName = "ValueName")]
    public string String { get; set; }

    public HashTestEnum Enum { get; set; }
    public DateTimeOffset DateTimeOffset { get; set; }
    public DateTime DateTime { get; set; }
    public List<int> PrimitiveCollection { get; set; }
    public List<HashTestClass> ComplexCollection { get; set; }
    public HashTestClass Object { get; set; }
}