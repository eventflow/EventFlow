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
using System.Linq;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.TestHelpers;
using EventFlow.Tests.UnitTests.Core.VersionedTypes;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.EventStores
{
    [Category(Categories.Unit)]
    public class EventDefinitionServiceTests : VersionedTypeDefinitionServiceTestSuite<EventDefinitionService, IAggregateEvent, EventVersionAttribute, EventDefinition>
    {
        [Test]
        public void GetDefinition_OnEventWithMultipleDefinitions_ThrowsException()
        {
            // Arrange
            Arrange_LoadAllTestTypes();

            // Act + Assert
            Assert.Throws<InvalidOperationException>(
                () => Sut.GetDefinition(typeof(MultiNamesEvent)));
        }

        [Test]
        public void GetDefinitions_OnEventWithMultipleDefinitions_ReturnsThemAll()
        {
            // Arrange
            Sut.Load(typeof(MultiNamesEvent));

            // Act
            var eventDefinitions = Sut.GetDefinitions(typeof(MultiNamesEvent));

            // Assert
            eventDefinitions.Should().HaveCount(3);
            eventDefinitions
                .Select(d => $"{d.Name}-V{d.Version}")
                .Should().BeEquivalentTo(new []{"multi-names-event-V1", "MultiNamesEvent-V1", "MultiNamesEvent-V2"});
        }

        [EventVersion("Fancy", 42)]
        public class TestEventWithLongName : AggregateEvent<IAggregateRoot<IIdentity>, IIdentity> { }
        
        [EventVersion("multi-names-event", 1)]
        [EventVersion("MultiNamesEvent", 1)]
        [EventVersion("MultiNamesEvent", 2)]
        public class MultiNamesEvent : AggregateEvent<IAggregateRoot<IIdentity>, IIdentity> { }

        public class TestEvent : AggregateEvent<IAggregateRoot<IIdentity>, IIdentity> { }

        public class TestEventV2 : AggregateEvent<IAggregateRoot<IIdentity>, IIdentity> { }

        public class OldTestEventV5 : AggregateEvent<IAggregateRoot<IIdentity>, IIdentity> { }

        public class OldThe5ThEventV4 : AggregateEvent<IAggregateRoot<IIdentity>, IIdentity> { }

        public override IEnumerable<VersionTypeTestCase> GetTestCases()
        {
            yield return new VersionTypeTestCase
                {
                    Name = "TestEvent",
                    Type = typeof(TestEvent),
                    Version = 1,
                };
            yield return new VersionTypeTestCase
                {
                    Name = "TestEvent",
                    Type = typeof(TestEventV2),
                    Version = 2,
                };
            yield return new VersionTypeTestCase
                {
                    Name = "TestEvent",
                    Type = typeof(OldTestEventV5),
                    Version = 5,
                };
            yield return new VersionTypeTestCase
                {
                    Name = "Fancy",
                    Type = typeof(TestEventWithLongName),
                    Version = 42,
                };
            yield return new VersionTypeTestCase
                {
                    Name = "The5ThEvent",
                    Type = typeof(OldThe5ThEventV4),
                    Version = 4,
                };
            
            // Multiple names to same events
            yield return new VersionTypeTestCase
                {
                    Name = "multi-names-event",
                    Type = typeof(MultiNamesEvent),
                    Version = 1,
                };
            yield return new VersionTypeTestCase
                {
                    Name = "MultiNamesEvent",
                    Type = typeof(MultiNamesEvent),
                    Version = 1,
                };
            yield return new VersionTypeTestCase
                {
                    Name = "MultiNamesEvent",
                    Type = typeof(MultiNamesEvent),
                    Version = 2,
                };
        }
    }
}