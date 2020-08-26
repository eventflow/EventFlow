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

using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using EventFlow.ValueObjects;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using System;

namespace EventFlow.Tests.UnitTests.Extensions
{
    [Category(Categories.Unit)]
    public class JsonSerializerExtensionTests
    {
        private class MyClass
        {
            public DateTime DateTime { get; set; }
        }

        private class MyClassConverter : JsonConverter<MyClass>
        {
            public override MyClass ReadJson(JsonReader reader, Type objectType, MyClass existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
            {
                return new MyClass() { DateTime = new DateTime((long)reader.Value) };
            }

            public override void WriteJson(JsonWriter writer, MyClass value, Newtonsoft.Json.JsonSerializer serializer)
            {
                serializer.Serialize(writer, value.DateTime.Ticks);
            }
        }

        private class MySingleValueObject : SingleValueObject<DateTime>
        {
            public MySingleValueObject(DateTime value) : base(value) { }
        }


        [Test]
        public void JsonSerializerCanBeConfigured()
        {
            // Arrange
            var resolver = EventFlowOptions.New
                .ConfigureJson(json => json
                    .AddSingleValueObjects()
                    .AddConverter<MyClassConverter>()
                )
                .CreateResolver();
            var serializer = resolver.Resolve<IJsonSerializer>();

            // Act
            var myClassSerialized = serializer.Serialize(new MyClass() { DateTime = new DateTime(1000000) });
            var myClassDeserialized = serializer.Deserialize<MyClass>(myClassSerialized);
            var svoSerialized = serializer.Serialize(new MySingleValueObject(new DateTime(1970, 1, 1)));
            var svoDeserialized = serializer.Deserialize<MySingleValueObject>(svoSerialized);

            // Assert
            myClassSerialized.Should().Be("1000000");
            myClassDeserialized.DateTime.Ticks.Should().Be(1000000);
            myClassDeserialized.DateTime.Ticks.Should().NotBe(10);
            svoDeserialized.Should().Be(new MySingleValueObject(new DateTime(1970, 1, 1)));
            svoDeserialized.Should().NotBe(new MySingleValueObject(new DateTime(2001, 1, 1)));
        }
    }
}
