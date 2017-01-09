// The MIT License (MIT)
// 
// Copyright (c) 2015-2017 Rasmus Mikkelsen
// Copyright (c) 2015-2017 eBay Software Foundation
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
using EventFlow.Aggregates;
using EventFlow.Extensions;
using FluentAssertions;
using NUnit.Framework;
using EventFlow.Configuration.Registrations;
using EventFlow.TestHelpers;
using System.Collections.Generic;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;

namespace EventFlow.Tests.UnitTests.Extensions
{
    [Category(Categories.Unit)]
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
            resolver.HasRegistrationFor<ThingyAggregate>().Should().Be(true);
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
            Action act = () => sut.AddAggregateRoots(new List<Type> { typeof(ThingyId) });

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

    public abstract class AbstractTestAggregate : AggregateRoot<ThingyAggregate, ThingyId>,
        IEmit<ThingyDomainErrorAfterFirstEvent>
    {
        protected AbstractTestAggregate(ThingyId id) : base(id)
        {
        }

        public void Apply(ThingyDomainErrorAfterFirstEvent aggregateEvent)
        {
        }
    }

    public class LocalTestAggregate : AbstractTestAggregate
    {
        public LocalTestAggregate(ThingyId id) : base(id)
        {
        }
    }
}
