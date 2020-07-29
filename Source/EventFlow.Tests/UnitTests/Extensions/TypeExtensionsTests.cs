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
using System.Collections.Generic;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Extensions
{
    [Category(Categories.Unit)]
    public class TypeExtensionsTests
    {
        [TestCase(typeof(string), "String")]
        [TestCase(typeof(int), "Int32")]
        [TestCase(typeof(IEnumerable<>), "IEnumerable<>")]
        [TestCase(typeof(KeyValuePair<,>), "KeyValuePair<,>")]
        [TestCase(typeof(IEnumerable<string>), "IEnumerable<String>")]
        [TestCase(typeof(IEnumerable<IEnumerable<string>>), "IEnumerable<IEnumerable<String>>")]
        [TestCase(typeof(KeyValuePair<bool,long>), "KeyValuePair<Boolean,Int64>")]
        [TestCase(typeof(KeyValuePair<KeyValuePair<bool, long>, KeyValuePair<bool, long>>), "KeyValuePair<KeyValuePair<Boolean,Int64>,KeyValuePair<Boolean,Int64>>")]
        public void PrettyPrint(Type type, string expectedPrettyPrint)
        {
            // Act
            var prettyPrint = type.PrettyPrint();

            // Assert
            prettyPrint.Should().Be(expectedPrettyPrint);
        }

        [TestCase(typeof(TestAggregateWithOutAttribute), "TestAggregateWithOutAttribute")]
        [TestCase(typeof(TestAggregateWithAttribute), "BetterNameForAggregate")]
        public void GetAggregateName(Type aggregateType, string expectedAggregateName)
        {
            // Act
            var aggregateName = aggregateType.GetAggregateName();

            // Assert
            aggregateName.Value.Should().Be(expectedAggregateName);
        }

        public class TestId : Identity<TestId>
        {
            public TestId(string value) : base(value)
            {
            }
        }

        public class TestAggregateWithOutAttribute : AggregateRoot<TestAggregateWithOutAttribute, TestId>
        {
            public TestAggregateWithOutAttribute(TestId id) : base(id)
            {
            }
        }

        [AggregateName("BetterNameForAggregate")]
        public class TestAggregateWithAttribute : AggregateRoot<TestAggregateWithOutAttribute, TestId>
        {
            public TestAggregateWithAttribute(TestId id) : base(id)
            {
            }
        }
    }
}