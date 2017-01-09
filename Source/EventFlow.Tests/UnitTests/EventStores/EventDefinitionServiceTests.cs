// The MIT License (MIT)
// 
// Copyright (c) 2015-2017 Rasmus Mikkelsen
// Copyright (c) 2015-2017 eBay Software Foundation
// https://github.com/rasmus/EventFlow
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
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.TestHelpers;
using EventFlow.Tests.UnitTests.Core.VersionedTypes;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.EventStores
{
    [Category(Categories.Unit)]
    public class EventDefinitionServiceTests : VersionedTypeDefinitionServiceTestSuite<EventDefinitionService, IAggregateEvent, EventVersionAttribute, EventDefinition>
    {
        [EventVersion("Fancy", 42)]
        public class TestEventWithLongName : AggregateEvent<IAggregateRoot<IIdentity>, IIdentity> { }

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
        }
    }
}