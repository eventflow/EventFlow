﻿// The MIT License (MIT)
//
// Copyright (c) 2015-2016 Rasmus Mikkelsen
// Copyright (c) 2015-2016 eBay Software Foundation
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
using System.Linq;
using EventFlow.Sagas;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.Sagas;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Sagas
{
    [Category(Categories.Unit)]
    public class SagaDefinitionServiceTests : TestsFor<SagaDefinitionService>
    {
        [TestCase(typeof(ThingySagaStartRequestedEvent))]
        [TestCase(typeof(ThingySagaCompleteRequestedEvent))]
        public void GetSagaTypeDetails_WithSubscribedAggregateEvents(Type aggregateEventType)
        {
            // Arrange
            Sut.LoadSagas(typeof(ThingySaga));

            // Act
            var sagaTypeDetails = Sut.GetSagaDetails(aggregateEventType).ToList();

            // Assert
            sagaTypeDetails.Should().HaveCount(1);
            sagaTypeDetails.Single().SagaType.Should().Be(typeof(ThingySaga));
        }

        [TestCase(typeof(ThingyDomainErrorAfterFirstEvent))]
        [TestCase(typeof(ThingyMessageAddedEvent))]
        public void GetSagaTypeDetails_WithUnknownAggregateEvents(Type aggregateEventType)
        {
            // Arrange
            Sut.LoadSagas(typeof(ThingySaga));

            // Act
            var sagaTypeDetails = Sut.GetSagaDetails(aggregateEventType).ToList();

            // Assert
            sagaTypeDetails.Should().BeEmpty();
        }
    }
}
