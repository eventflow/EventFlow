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
            // arrange
            var registry = new AutofacServiceRegistration();
            var sut = EventFlowOptions.New
                .UseServiceRegistration(registry);

            // act
            sut.AddAggregateRoots(EventFlowTests.Assembly);

            // assert
            registry.HasRegistrationFor<AbstractTestAggregate>().Should().Be(false);
        }

        [Test]
        public void ClosedIAggregateRootImplementationIsSelected()
        {
            // arrange
            var registry = new AutofacServiceRegistration();
            var sut = EventFlowOptions.New
                .UseServiceRegistration(registry);

            // act
            sut.AddAggregateRoots(EventFlowTestHelpers.Assembly);

            // assert
            registry.HasRegistrationFor<TestAggregate>().Should().Be(true);
        }

        [Test]
        public void AbstractAggregateRootImplementationIsRejected()
        {
            // arrange
            var registry = new AutofacServiceRegistration();
            var sut = EventFlowOptions.New
                .UseServiceRegistration(registry);

            // act
            Action act = () => sut.AddAggregateRoots(new List<Type> { typeof(AbstractTestAggregate) } );

            // assert
            act.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void NonIAggregateRootImplementationIsRejected()
        {
            // arrange
            var registry = new AutofacServiceRegistration();
            var sut = EventFlowOptions.New
                .UseServiceRegistration(registry);

            // act
            Action act = () => sut.AddAggregateRoots(new List<Type> { typeof(TestId) });

            // assert
            act.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void ClosedIAggregateRootImplementationIsAccepted()
        {
            // arrange
            var registry = new AutofacServiceRegistration();
            var sut = EventFlowOptions.New
                .UseServiceRegistration(registry);

            // act
            Action act = () => sut.AddAggregateRoots(new List<Type> { typeof(LocalTestAggregate) });

            // assert
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
