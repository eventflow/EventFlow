using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using EventFlow.ValueObjects;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
