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

using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Snapshots;
using EventFlow.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace EventFlow.TestHelpers.Suites
{
    /// <summary>
    /// Provider-agnostic conformance suite that every <see cref="IJsonSerializer"/>
    /// implementation must satisfy. Concrete test fixtures supply the serializer under
    /// test by overriding <see cref="CreateSerializer"/>.
    /// </summary>
    [Category(Categories.Unit)]
    public abstract class TestSuiteForJsonSerializer
    {
        protected abstract IJsonSerializer CreateSerializer();

        private IJsonSerializer _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = CreateSerializer();
        }

        [Test]
        public void SingleValueObject_String_RoundTrips()
        {
            var json = _sut.Serialize(new StringSvo("abc"));

            var result = _sut.Deserialize<StringSvo>(json);

            result.Value.ShouldBe("abc");
        }

        [Test]
        public void SingleValueObject_Int_RoundTrips()
        {
            var json = _sut.Serialize(new IntSvo(42));

            var result = _sut.Deserialize<IntSvo>(json);

            result.Value.ShouldBe(42);
        }

        [Test]
        public void Deserialize_NonGeneric_ReturnsTypedInstance()
        {
            var json = _sut.Serialize(new IntSvo(7));

            var result = _sut.Deserialize(json, typeof(IntSvo));

            result.ShouldBeOfType<IntSvo>();
            ((IntSvo) result).Value.ShouldBe(7);
        }

        [Test]
        public void Metadata_RoundTripsTypedAccessors()
        {
            var metadata = new Metadata
            {
                AggregateName = "ThingyAggregate",
                AggregateSequenceNumber = 3,
            };

            var json = _sut.Serialize(metadata);
            var result = _sut.Deserialize<Metadata>(json);

            result.AggregateName.ShouldBe("ThingyAggregate");
            result.AggregateSequenceNumber.ShouldBe(3);
        }

        [Test]
        public void Metadata_SerializesUnderlyingKeysNotComputedProperties()
        {
            var metadata = new Metadata
            {
                AggregateName = "ThingyAggregate",
            };

            var json = _sut.Serialize(metadata);

            // The container is a dictionary; the metadata key is emitted, not the CLR
            // accessor name "AggregateName".
            json.ShouldContain(MetadataKeys.AggregateName);
            json.ShouldNotContain(nameof(Metadata.AggregateName));
        }

        [Test]
        public void SnapshotMetadata_RoundTripsTypedAccessors()
        {
            var metadata = new SnapshotMetadata
            {
                AggregateName = "ThingyAggregate",
                AggregateSequenceNumber = 3,
                SnapshotName = "ThingySnapshot",
                SnapshotVersion = 2,
            };

            var json = _sut.Serialize(metadata);
            var result = _sut.Deserialize<SnapshotMetadata>(json);

            result.AggregateName.ShouldBe("ThingyAggregate");
            result.AggregateSequenceNumber.ShouldBe(3);
            result.SnapshotName.ShouldBe("ThingySnapshot");
            result.SnapshotVersion.ShouldBe(2);
        }

        [Test]
        public void SnapshotMetadata_SerializesUnderlyingKeysNotComputedProperties()
        {
            var metadata = new SnapshotMetadata
            {
                AggregateName = "ThingyAggregate",
                SnapshotName = "ThingySnapshot",
            };

            var json = _sut.Serialize(metadata);

            // The container is a dictionary; the snapshot metadata keys are emitted, not
            // the CLR accessor names. The [JsonIgnore] attributes that used to sit on the
            // computed properties were inert under the dictionary contract, so removing
            // them preserves this behavior.
            json.ShouldContain(SnapshotMetadataKeys.AggregateName);
            json.ShouldContain(SnapshotMetadataKeys.SnapshotName);
            json.ShouldNotContain(nameof(SnapshotMetadata.AggregateName));
            json.ShouldNotContain(nameof(SnapshotMetadata.SnapshotName));
        }

        [Test]
        public void Serialize_Indented_DiffersFromCompact()
        {
            var value = new ComplexValue {Name = "a", Number = 1};

            var compact = _sut.Serialize(value);
            var indented = _sut.Serialize(value, indented: true);

            indented.ShouldNotBe(compact);
            indented.ShouldContain("\n");
            compact.ShouldNotContain("\n");
        }

        public class StringSvo : SingleValueObject<string>
        {
            public StringSvo(string value) : base(value) { }
        }

        public class IntSvo : SingleValueObject<int>
        {
            public IntSvo(int value) : base(value) { }
        }

        public class ComplexValue
        {
            public string Name { get; set; }
            public int Number { get; set; }
        }
    }
}
