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
using System.Linq;
using EventFlow.TestHelpers;
using EventFlow.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.ValueObjects
{
    [Category(Categories.Unit)]
    public class ValueObjectTests : Test
    {
        public class StringObject : ValueObject
        {
            public string StringValue { get; set; }
        }

        public class ListObject : ValueObject
        {
            public List<StringObject> StringValues { get; set; }

            public ListObject(params string[] strings)
            {
                StringValues = strings.Select(s => new StringObject{StringValue = s}).ToList();
            }

            protected override IEnumerable<object> GetEqualityComponents()
            {
                return StringValues;
            }
        }

        [Test]
        public void SameStringObjectsAreEqual()
        {
            // Arrange
            var str = A<string>();
            var stringObject1 = new StringObject { StringValue = str };
            var stringObject2 = new StringObject { StringValue = str };

            // Assert
            stringObject1.GetHashCode().Should().Be(stringObject2.GetHashCode());
            stringObject1.Equals(stringObject2).Should().BeTrue();
            (stringObject1 == stringObject2).Should().BeTrue();
        }

        [Test]
        public void DifferentStringObjectsAreNotEqual()
        {
            // Arrange
            var stringObject1 = new StringObject { StringValue = A<string>() };
            var stringObject2 = new StringObject { StringValue = A<string>() };

            // Assert
            stringObject1.GetHashCode().Should().NotBe(stringObject2.GetHashCode());
            stringObject1.Equals(stringObject2).Should().BeFalse();
            (stringObject1 == stringObject2).Should().BeFalse();
        }

        [Test]
        public void SameListObjectsAreEqual()
        {
            // Arrange
            var values = Many<string>().ToArray();
            var listObject1 = new ListObject(values);
            var listObject2 = new ListObject(values);

            // Assert
            listObject1.GetHashCode().Should().Be(listObject2.GetHashCode(), "hash code");
            listObject1.Equals(listObject2).Should().BeTrue("Equals");
            (listObject1 == listObject2).Should().BeTrue("==");
        }

        [Test]
        public void DifferentListObjectsAreNotEqual()
        {
            // Arrange
            var listObject1 = new ListObject(Many<string>().ToArray());
            var listObject2 = new ListObject(Many<string>().ToArray());

            // Assert
            listObject1.GetHashCode().Should().NotBe(listObject2.GetHashCode(), "hash code");
            listObject1.Equals(listObject2).Should().BeFalse("Equals");
            (listObject1 == listObject2).Should().BeFalse("==");
        }
    }
}
