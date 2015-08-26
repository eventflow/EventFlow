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

using EventFlow.Aggregates;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EventFlow.Tests.UnitTests.Aggregates
{
    public class AggregateIdWithValidatorTests
    {
        [Test]
        public void CreatedIsSame()
        {
            // Act
            var id1 = UserId.New("bob@test.domain");
            var id2 = UserId.New("bob@test.domain");

            // Assert
            id1.Value.Should().Be(id2.Value);
        }

        [Test]
        public void DifferentAreNotEqual()
        {
            // Arrange
            var id1 = UserId.With("user-bob@test.domain");
            var id2 = UserId.With("user-sally@test.domain");

            // Assert
            id1.Equals(id2).Should().BeFalse();
            (id1 == id2).Should().BeFalse();
        }

        [Test]
        public void ManuallyCreatedIsOk()
        {
            // Arrange
            const string value = "user-bob@test.domain";

            // Act
            var testId = UserId.With(value);

            // Test
            testId.Value.Should().Be(value);
        }

        [Test]
        public void SameIdsAreEqual()
        {
            // Arrange
            const string value = "user-bob@test.domain";
            var id1 = UserId.With(value);
            var id2 = UserId.With(value);

            // Assert
            id1.Equals(id2).Should().BeTrue();
            (id1 == id2).Should().BeTrue();
        }

        [Test]
        public void ValidorRejectsInvalidValue()
        {
            // arrange
            Action act = () => UserId.New("bobtest.com");

            // act+assert
            act.ShouldThrow<Exception>();
        }
    }

    public class UserId : Identity<UserId, StringIdentityComposer, SimpleEmailIdentityValidator>
    {
        public UserId(string value)
            : base(value)
        {
        }
    }


    public class StringIdentityComposer : IIdentityComposer
    {
        public string Create(string value) => value;
    }

    public class SimpleEmailIdentityValidator : IIdentityValidator
    {
        public bool IsValid(string context, string value) => !Validate(context, value).Any();

        public IEnumerable<string> Validate(string context, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                yield return string.Format("Aggregate ID is null or empty");
                yield break;
            }

            var parts = value.Split('-');
            if (parts.Length < 2)
            {
                yield return string.Format("Aggregate ID requires a prefix and none found");
                yield break;
            }

            if (!parts[1].Contains("@"))
            {
                yield return string.Format("Aggregate ID does not appear to be an email address");
                yield break;
            }
        }
    }
}
