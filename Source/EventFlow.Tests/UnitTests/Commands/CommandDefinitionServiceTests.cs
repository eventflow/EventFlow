// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
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

using System;
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Core;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Commands
{
    [TestFixture]
    public class CommandDefinitionServiceTests : TestsFor<CommandDefinitionService>
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

        [TestCase(typeof(TestCommand), 1, "TestCommand")]
        [TestCase(typeof(TestCommandV2), 2, "TestCommand")]
        [TestCase(typeof(OldTestCommandV5), 5, "TestCommand")]
        [TestCase(typeof(TestCommandWithLongName), 42, "Fancy")]
        public void GetCommandDefinition_EventWithVersion(Type commandType, int expectedVersion, string expectedName)
        {
            // Act
            var commandDefinition = Sut.GetCommandDefinition(commandType);

            // Assert
            commandDefinition.Name.Should().Be(expectedName);
            commandDefinition.Version.Should().Be(expectedVersion);
            commandDefinition.Type.Should().Be(commandType);
        }

        [TestCase("TestCommand", 1, typeof(TestCommand))]
        [TestCase("TestCommand", 2, typeof(TestCommandV2))]
        [TestCase("TestCommand", 5, typeof(OldTestCommandV5))]
        [TestCase("Fancy", 42, typeof(TestCommandWithLongName))]
        public void LoadCommandFollowedByGetCommandDefinition_ReturnsCorrectAnswer(string commandName, int commandVersion, Type expectedCommandType)
        {
            // Arrange
            Sut.LoadCommands(new []
                {
                    typeof(TestCommand),
                    typeof(TestCommandV2),
                    typeof(OldTestCommandV5),
                    typeof(TestCommandWithLongName)
                });

            // Act
            var commandDefinition = Sut.GetCommandDefinition(commandName, commandVersion);

            // Assert
            commandDefinition.Name.Should().Be(commandName);
            commandDefinition.Version.Should().Be(commandVersion);
            commandDefinition.Type.Should().Be(expectedCommandType);
        }

        [Test]
        public void CanLoadNull()
        {
            // Act
            Sut.LoadCommands(null);
        }
    }
}
