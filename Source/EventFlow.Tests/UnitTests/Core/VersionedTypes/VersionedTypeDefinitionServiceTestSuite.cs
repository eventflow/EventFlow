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
using EventFlow.Core.VersionedTypes;
using EventFlow.TestHelpers;
using AutoFixture;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Core.VersionedTypes
{
    public abstract class VersionedTypeDefinitionServiceTestSuite<TSut, TTypeCheck, TAttribute, TDefinition> : TestsFor<TSut>
        where TSut : VersionedTypeDefinitionService<TTypeCheck, TAttribute, TDefinition>
        where TAttribute : VersionedTypeAttribute
        where TDefinition : VersionedTypeDefinition
    {
        public class VersionTypeTestCase
        {
            public Type Type { get; set; }
            public int Version { get; set; }
            public string Name { get; set; }
        }

        [Test]
        public void GetDefinition_WithValidNameAndVersion_ReturnsCorrectAnswer_Cases()
        {
            // TODO: Redesign this, NUnit 3 enforces TestCaseSource(...) to reference static
            foreach (var versionTypeTestCase in GetTestCases())
            {
                GetDefinition_WithValidNameAndVersion_ReturnsCorrectAnswer(versionTypeTestCase);
            }
        }

        public void GetDefinition_WithValidNameAndVersion_ReturnsCorrectAnswer(VersionTypeTestCase testCase)
        {
            // Arrange
            Arrange_LoadAllTestTypes();

            // Act
            var eventDefinition = Sut.GetDefinition(testCase.Name, testCase.Version);

            // Assert
            eventDefinition.Name.Should().Be(testCase.Name);
            eventDefinition.Version.Should().Be(testCase.Version);
            eventDefinition.Type.Should().Be(testCase.Type);
        }

        [Test]
        public void GetDefinition_WithValidType_ReturnsCorrectAnswer_Cases()
        {
            // TODO: Redesign this, NUnit 3 enforces TestCaseSource(...) to reference static
            foreach (var versionTypeTestCase in GetTestCases())
            {
                GetDefinition_WithValidType_ReturnsCorrectAnswer(versionTypeTestCase);
            }
        }

        private void GetDefinition_WithValidType_ReturnsCorrectAnswer(VersionTypeTestCase testCase)
        {
            // Arrange
            Arrange_LoadAllTestTypes();

            // Act
            var eventDefinitions = Sut.GetDefinitions(testCase.Type);

            // Assert
            var hasIt = eventDefinitions.SingleOrDefault(e =>
                e.Name == testCase.Name &&
                e.Version == testCase.Version &&
                e.Type == testCase.Type);
            hasIt.Should().NotBeNull();
        }

        [Test]
        public void GetDefinitions_WithName_ReturnsList()
        {
            // Assert
            Arrange_LoadAllTestTypes();
            var nameWithMultipleDefinitions = GetTestCases()
                .GroupBy(c => c.Name)
                .Where(g => g.Count() > 1)
                .OrderByDescending(g => g.Count())
                .First().Key;

            // Assert
            var result = Sut.GetDefinitions(nameWithMultipleDefinitions).ToList();

            // Assert
            result.Should().HaveCount(i => i > 1);
            result.Should().OnlyContain(d => d.Name == nameWithMultipleDefinitions);
        }

        [Test]
        public void GetDefinitionShouldFailForUnknownEvents()
        {
            // Act + Assert
            Assert.Throws<ArgumentException>(() => Sut.GetDefinition(GetTestCases().First().Type));
        }

        [Test]
        public void CanLoadSameEventMultipleTimes()
        {
            // Arrange
            var types = GetTestCases().Select(c => c.Type).ToList();

            // Act
            Assert.DoesNotThrow(() =>
                {
                    Sut.Load(types);
                    Sut.Load(types);
                });
        }

        [Test]
        public void TryGetDefinition_WithInvalidName_ReturnsFalse()
        {
            // Arrange
            TDefinition definition;

            // Act
            var found = Sut.TryGetDefinition(Fixture.Create<string>(), 0, out definition);

            // Assert
            found.Should().BeFalse();
        }

        [Test]
        public void TryGetDefinition_WithInvalidType_ReturnsFalse()
        {
            // Arrange
            TDefinition definition;

            // Act
            var found = Sut.TryGetDefinition(typeof(object), out definition);

            // Assert
            found.Should().BeFalse();
        }

        [Test]
        public void GetDefinition_WithInvalidType_ThrowsException()
        {
            // Act + Assert
            Assert.Throws<ArgumentException>(() => Sut.GetDefinition(typeof(object)));
        }

        [Test]
        public void GetDefinition_WithInvalidName_ThrowsException()
        {
            // Act + Assert
            Assert.Throws<ArgumentException>(() => Sut.GetDefinition(Fixture.Create<string>(), 0));
        }

        [Test]
        public void GetDefinitions_WithInvalidName_ReturnsEmpty()
        {
            // Act
            var result = Sut.GetDefinitions(Fixture.Create<string>());

            // Assert
            result.Should().BeEmpty();
        }

        [Test]
        public void GetAllDefinitions_WhenNoneLoaded_IsEmpty()
        {
            // Act
            var result = Sut.GetAllDefinitions();

            // Assert
            result.Should().BeEmpty();
        }

        [Test]
        public void GetAllDefinitions_WhenAllLoaded_ReturnsAll()
        {
            // Arrange
            var expectedTypes = Arrange_LoadAllTestTypes();

            // Act
            var result = Sut.GetAllDefinitions().Select(d => d.Type)
                .Distinct()
                .ToList();

            // Assert
            result.Should().BeEquivalentTo(expectedTypes);
        }

        [Test]
        public void Load_CalledWithInvalidType_ThrowsException()
        {
            // Act + Assert
            Assert.Throws<ArgumentException>(() => Sut.Load(typeof(object)));
        }

        [Test]
        public void CanLoadNull()
        {
            // Act + Assert
            Assert.DoesNotThrow(() => Sut.Load(null));
        }

        protected IReadOnlyCollection<Type> Arrange_LoadAllTestTypes()
        {
            var types = GetTestCases()
                .Select(t => t.Type)
                .Distinct()
                .ToList();
            Sut.Load(types);
            return types;
        }

        public abstract IEnumerable<VersionTypeTestCase> GetTestCases();
    }
}