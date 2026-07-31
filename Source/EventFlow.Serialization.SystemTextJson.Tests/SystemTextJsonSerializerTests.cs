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

using System.Text.Json.Serialization;
using EventFlow.Core;
using EventFlow.TestHelpers.Suites;
using NUnit.Framework;
using Shouldly;

namespace EventFlow.Serialization.SystemTextJson.Tests
{
    [TestFixture]
    public class SystemTextJsonSerializerTests : TestSuiteForJsonSerializer
    {
        protected override IJsonSerializer CreateSerializer()
        {
            return new SystemTextJsonSerializer();
        }

        [Test]
        public void JsonPropertyName_RenamesPropertyOnSerialize()
        {
            var sut = CreateSerializer();

            var json = sut.Serialize(new WithAttributes {Renamed = "value"});

            json.ShouldContain("\"custom_name\"");
            json.ShouldNotContain(nameof(WithAttributes.Renamed));
        }

        [Test]
        public void JsonPropertyName_RenamedPropertyRoundTrips()
        {
            var sut = CreateSerializer();

            var json = sut.Serialize(new WithAttributes {Renamed = "value"});
            var result = sut.Deserialize<WithAttributes>(json);

            result.Renamed.ShouldBe("value");
        }

        [Test]
        public void JsonIgnore_OmitsPropertyOnSerialize()
        {
            var sut = CreateSerializer();

            var json = sut.Serialize(new WithAttributes {Ignored = "secret"});

            json.ShouldNotContain("secret");
            json.ShouldNotContain(nameof(WithAttributes.Ignored));
        }

        [Test]
        public void JsonIgnore_IgnoredPropertyIsNotRestoredOnDeserialize()
        {
            var sut = CreateSerializer();

            var json = sut.Serialize(new WithAttributes {Ignored = "secret"});
            var result = sut.Deserialize<WithAttributes>(json);

            result.Ignored.ShouldBeNull();
        }

        public class WithAttributes
        {
            [JsonPropertyName("custom_name")]
            public string Renamed { get; set; }

            [JsonIgnore]
            public string Ignored { get; set; }
        }
    }
}
