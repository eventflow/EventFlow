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

using System.Collections.Generic;
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Core;
using EventFlow.TestHelpers;
using EventFlow.Tests.UnitTests.Core.VersionedTypes;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Commands
{
    [TestFixture]
    [Category(Categories.Unit)]
    public class CommandDefinitionServiceTests : VersionedTypeDefinitionServiceTestSuite<CommandDefinitionService, ICommand, CommandVersionAttribute, CommandDefinition>
    {
        [CommandVersion("Fancy", 42)]
        public class TestCommandWithLongName : Command<IAggregateRoot<IIdentity>, IIdentity>
        {
            public TestCommandWithLongName(IIdentity aggregateId) : base(aggregateId) { }
        }

        public class TestCommand : Command<IAggregateRoot<IIdentity>, IIdentity>
        {
            public TestCommand(IIdentity aggregateId) : base(aggregateId) { }
        }

        public class TestCommandV2 : Command<IAggregateRoot<IIdentity>, IIdentity>
        {
            public TestCommandV2(IIdentity aggregateId) : base(aggregateId) { }
        }

        public class OldTestCommandV5 : Command<IAggregateRoot<IIdentity>, IIdentity>
        {
            public OldTestCommandV5(IIdentity aggregateId) : base(aggregateId) { }
        }

        public override IEnumerable<VersionTypeTestCase> GetTestCases()
        {
            yield return new VersionTypeTestCase
                {
                    Name = "TestCommand",
                    Type = typeof(TestCommand),
                    Version = 1,
                };
            yield return new VersionTypeTestCase
                {
                    Name = "TestCommand",
                    Type = typeof(TestCommandV2),
                    Version = 2,
                };
            yield return new VersionTypeTestCase
                {
                    Name = "TestCommand",
                    Type = typeof(OldTestCommandV5),
                    Version = 5,
                };
            yield return new VersionTypeTestCase
                {
                    Name = "Fancy",
                    Type = typeof(TestCommandWithLongName),
                    Version = 42,
                };
        }
    }
}