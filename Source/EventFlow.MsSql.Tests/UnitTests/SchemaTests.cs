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