// The MIT License (MIT)
// 
// Copyright (c) 2015 Rasmus Mikkelsen
// Copyright (c) 2015 eBay Software Foundation
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
// 

using System;
using System.Collections.Generic;
using System.Linq;
using EventFlow.Core.VersionedTypes;
using EventFlow.TestHelpers;
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

        [TestCaseSource(nameof(GetTestCases))]
        public void Load_FollowedBy_GetEventDefinition_ReturnsCorrectAnswer(VersionTypeTestCase testCase)
        {
            // Arrange
            Sut.Load(GetTestCases().Select(t => t.Type).ToList());

            // Act
            var eventDefinition = Sut.GetDefinition(testCase.Name, testCase.Version);

            // Assert
            eventDefinition.Name.Should().Be(testCase.Name);
            eventDefinition.Version.Should().Be(testCase.Version);
            eventDefinition.Type.Should().Be(testCase.Type);
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
        public void Load_CalledWithInvalidType_ThrowsException()
        {
            // Act + Assert
            Assert.Throws<ArgumentException>(() => Sut.Load(typeof (object)));
        }

        [Test]
        public void CanLoadNull()
        {
            // Act + Assert
            Assert.DoesNotThrow(() => Sut.Load(null));
        }

        public abstract IEnumerable<VersionTypeTestCase> GetTestCases();
    }
}