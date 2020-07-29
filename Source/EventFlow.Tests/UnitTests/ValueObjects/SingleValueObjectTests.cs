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

using System;
using System.Linq;
using EventFlow.TestHelpers;
using EventFlow.ValueObjects;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.ValueObjects
{
    [Category(Categories.Unit)]
    public class SingleValueObjectTests : Test
    {
        public class StringSingleValue : SingleValueObject<string>
        {
            public StringSingleValue(string value) : base(value) { }
        }

        public enum MagicEnum
        {
            Two = 2,
            Zero = 0,
            Three = 3,
            One = 1
        }

        public class MagicEnumSingleValue : SingleValueObject<MagicEnum>
        {
            public MagicEnumSingleValue(MagicEnum value) : base(value) { }
        }

        [Test]
        public void Ordering()
        {
            // Arrange
            var values = Many<string>(10);
            var orderedValues = values.OrderBy(s => s).ToList();
            values.Should().NotEqual(orderedValues); // Data test
            var singleValueObjects = values.Select(s => new StringSingleValue(s)).ToList();

            // Act
            var orderedSingleValueObjects = singleValueObjects.OrderBy(v => v).ToList();

            // Assert
            orderedSingleValueObjects.Select(v => v.Value).Should().BeEquivalentTo(
                orderedValues,
                o => o.WithStrictOrdering());
        }

        [Test]
        public void EnumOrdering()
        {
            // Arrange
            var values = Many<MagicEnum>(10);
            var orderedValues = values.OrderBy(s => s).ToList();
            values.Should().NotEqual(orderedValues); // Data test
            var singleValueObjects = values.Select(s => new MagicEnumSingleValue(s)).ToList();

            // Act
            var orderedSingleValueObjects = singleValueObjects.OrderBy(v => v).ToList();

            // Assert
            orderedSingleValueObjects.Select(v => v.Value).Should().BeEquivalentTo(
                orderedValues,
                o => o.WithStrictOrdering());
        }

        [Test]
        public void ProtectAgainsInvalidEnumValues()
        {
            // Act + Assert
            // ReSharper disable once ObjectCreationAsStatement
            var exception = Assert.Throws<ArgumentException>(() => new MagicEnumSingleValue((MagicEnum)42));
            exception.Message.Should().Be("The value '42' isn't defined in enum 'MagicEnum'");
        }

        [Test]
        public void EnumOrderingManual()
        {
            // Arrange
            var values = new[]
                {
                    new MagicEnumSingleValue(MagicEnum.Zero), 
                    new MagicEnumSingleValue(MagicEnum.Three), 
                    new MagicEnumSingleValue(MagicEnum.One), 
                    new MagicEnumSingleValue(MagicEnum.Two), 
                };
            
            // Act
            var orderedValues = values
                .OrderBy(v => v)
                .Select(v => v.Value)
                .ToList();
            
            // Assert
            orderedValues.Should().BeEquivalentTo(
                new []
                {
                    MagicEnum.Zero,
                    MagicEnum.One,
                    MagicEnum.Two,
                    MagicEnum.Three,
                },
                o => o.WithStrictOrdering());
        }

        [Test]
        public void NullEquals()
        {
            // Arrange
            var obj = new StringSingleValue(A<string>());
            var null_ = null as StringSingleValue;

            // Assert
            // ReSharper disable once ExpressionIsAlwaysNull
            obj.Equals(null_).Should().BeFalse();
        }

        [Test]
        public void EqualsForSameValues()
        {
            // Arrange
            var value = A<string>();
            var obj1 = new StringSingleValue(value);
            var obj2 = new StringSingleValue(value);

            // Assert
            (obj1 == obj2).Should().BeTrue();
            obj1.Equals(obj2).Should().BeTrue();
        }

        [Test]
        public void EqualsForDifferentValues()
        {
            // Arrange
            var value1 = A<string>();
            var value2 = A<string>();
            var obj1 = new StringSingleValue(value1);
            var obj2 = new StringSingleValue(value2);

            // Assert
            (obj1 == obj2).Should().BeFalse();
            obj1.Equals(obj2).Should().BeFalse();
        }

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };

        [JsonConverter(typeof(SingleValueObjectConverter))]
        public class IntSingleValue : SingleValueObject<int>
        {
            public IntSingleValue(int value) : base(value) { }
        }

        public class WithNullableIntSingleValue
        {
            public IntSingleValue I { get; }

            public WithNullableIntSingleValue(
                IntSingleValue i)
            {
                I = i;
            }
        }

        [Test]
        public void DeserializeNullableIntWithoutValue()
        {
            // Arrange
            var json = JsonConvert.SerializeObject(new { });

            // Act
            var with = JsonConvert.DeserializeObject<WithNullableIntSingleValue>(json);

            // Assert
            with.Should().NotBeNull();
            with.I.Should().BeNull();
        }

        [Test]
        public void DeserializeNullableIntWithNullValue()
        {
            // Arrange
            var json = "{\"i\":null}";

            // Act
            var with = JsonConvert.DeserializeObject<WithNullableIntSingleValue>(json);

            // Assert
            with.Should().NotBeNull();
            with.I.Should().BeNull();
        }

        [Test]
        public void DeserializeNullableIntWithValue()
        {
            // Arrange
            var i = A<int>();
            var json = JsonConvert.SerializeObject(new { i });

            // Act
            var with = JsonConvert.DeserializeObject<WithNullableIntSingleValue>(json);

            // Assert
            with.Should().NotBeNull();
            with.I.Value.Should().Be(i);
        }

        [Test]
        public void SerializeNullableIntWithoutValue()
        {
            // Arrange
            var with = new WithNullableIntSingleValue(null);

            // Act
            var json = JsonConvert.SerializeObject(with, Settings);

            // Assert
            json.Should().Be("{}");
        }

        [Test]
        public void SerializeNullableIntWithValue()
        {
            // Arrange
            var with = new WithNullableIntSingleValue(new IntSingleValue(42));

            // Act
            var json = JsonConvert.SerializeObject(with, Settings);

            // Assert
            json.Should().Be("{\"I\":42}");
        }
    }
}
