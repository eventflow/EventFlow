// The MIT License (MIT)
// 
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
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
using NUnit.Framework;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;
using System.Text.Json;

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
            public override MyClass Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return new MyClass() { DateTime = new DateTime((long)reader.GetDecimal()) };
            }

            public override void Write(Utf8JsonWriter writer, MyClass value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value.DateTime.Ticks);
            }
        }

        private class MySingleValueObject : SingleValueObject<DateTime>
        {
            public MySingleValueObject(DateTime value) : base(value) { }
        }


        [Test]
        public void JsonSerializerCanBeConfigured()
        {
            using (var serviceProvider = EventFlowOptions.New()
                .ConfigureJson(json => json
                    .AddSingleValueObjects()
                    .Converters.Add(new MyClassConverter())
                )
                .ServiceCollection.BuildServiceProvider())
            {
                // Arrange
                var serializer = serviceProvider.GetRequiredService<IJsonSerializer>();

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
}
