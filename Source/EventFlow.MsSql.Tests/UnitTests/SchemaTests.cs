// The MIT License (MIT)
// 
// Copyright (c) 2015-2025 Rasmus Mikkelsen
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
using EventFlow.TestHelpers;
using NUnit.Framework;

namespace EventFlow.MsSql.Tests.UnitTests
{
    [Category(Categories.Unit)]
    public class SchemaTests
    {
        [TestCase("dbo")]
        [TestCase("eventflow")]
        [TestCase("_eventflow")]
        [TestCase("schema1")]
        [TestCase("a@b$c#d_e")]
        public void ValidSchemaNameDoesNotThrow(string value)
        {
            Assert.DoesNotThrow(() => new Schema(value));
        }

        [Test]
        public void MaximumLengthSchemaNameDoesNotThrow()
        {
            var value = new string('a', 128);
            Assert.DoesNotThrow(() => new Schema(value));
        }

        [TestCase("")]
        [TestCase("1eventflow")]
        [TestCase("event-flow")]
        [TestCase("event flow")]
        public void InvalidSchemaNameThrowsArgumentException(string value)
        {
            Assert.Throws<ArgumentException>(() => new Schema(value));
        }

        [Test]
        public void SchemaNameLongerThanMaximumLengthThrowsArgumentException()
        {
            var value = new string('a', 129);
            Assert.Throws<ArgumentException>(() => new Schema(value));
        }
    }
}