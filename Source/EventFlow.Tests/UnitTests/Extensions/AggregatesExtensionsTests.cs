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
using EventFlow.Aggregates;
using EventFlow.Extensions;
using EventFlow.TestHelpers.Aggregates.Test;
using EventFlow.TestHelpers.Aggregates.Test.Events;
using FluentAssertions;
using NUnit.Framework;
using EventFlow.Configuration.Registrations;
using EventFlow.TestHelpers;
using System.Collections.Generic;

namespace EventFlow.Tests.UnitTests.Extensions
{
    public class AggregatesExtensionsTests
    {
        [Test]
        public void AbstractAggregateRootImplementationIsNotSelected()
        {
            // Arrange
            var registry = new AutofacServiceRegistration();
            var sut = EventFlowOptions.New
                .UseServiceRegistration(registry);

            // Act
            sut.AddAggregateRoots(EventFlowTests.Assembly);

            // Assert
            var resolver = sut.CreateResolver(false);
            resolver.HasRegistrationFor<AbstractTestAggregate>().Should().Be(false);
        }

        [Test]
        public void ClosedIAggregateRootImplementationIsSelected()
        {
            // Arrange
            var registry = new AutofacServiceRegistration();
            var sut = EventFlowOptions.New
                .UseServiceRegistration(registry);

            // Act
            sut.AddAggregateRoots(EventFlowTestHelpers.Assembly);

            // Assert
            var resolver = sut.CreateResolver(false);
            resolver.HasRegistrationFor<TestAggregate>().Should().Be(true);
        }

        [Test]
        public void AbstractAggregateRootImplementationIsRejected()
        {
            // Arrange
            var registry = new AutofacServiceRegistration();
            var sut = EventFlowOptions.New
                .UseServiceRegistration(registry);

            // Act
            Action act = () => sut.AddAggregateRoots(new List<Type> { typeof(AbstractTestAggregate) } );

            // Assert
            act.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void NonIAggregateRootImplementationIsRejected()
        {
            // Arrange
            var registry = new AutofacServiceRegistration();
            var sut = EventFlowOptions.New
                .UseServiceRegistration(registry);

            // Act
            Action act = () => sut.AddAggregateRoots(new List<Type> { typeof(TestId) });

            // Assert
            act.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void ClosedIAggregateRootImplementationIsAccepted()
        {
            // Arrange
            var registry = new AutofacServiceRegistration();
            var sut = EventFlowOptions.New
                .UseServiceRegistration(registry);

            // Act
            Action act = () => sut.AddAggregateRoots(new List<Type> { typeof(LocalTestAggregate) });

            // Assert
            act.ShouldNotThrow<ArgumentException>();
        }
    }

    public abstract class AbstractTestAggregate : AggregateRoot<TestAggregate, TestId>,
        IEmit<DomainErrorAfterFirstEvent>
    {
        public AbstractTestAggregate(TestId id) : base(id)
        {
        }

        public void Apply(DomainErrorAfterFirstEvent aggregateEvent)
        {
        }
    }

    public class LocalTestAggregate : AbstractTestAggregate
    {
        public LocalTestAggregate(TestId id) : base(id)
        {
        }
    }
}
