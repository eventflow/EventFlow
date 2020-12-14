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

using EventFlow.Examples.Shipping.Domain.Model.CargoModel.Entities;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.Specifications;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel.Entities;
using EventFlow.TestHelpers;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;

namespace EventFlow.Examples.Shipping.Tests.UnitTests.Domain.Model.CargoModel.Speficications
{
    [Category(Categories.Unit)]
    public class TransportLegsAreConnectedSpecificationTests : Test
    {
        [Test]
        public void Valid()
        {
            // Arrange
            var sut = new TransportLegsAreConnectedSpecification();
            var transportLegs = new[]
                {
                    new TransportLeg(TransportLegId.New, Locations.NewYork, Locations.Dallas, 1.January(2000), 2.January(2000), A<VoyageId>(), CarrierMovementId.New),
                    new TransportLeg(TransportLegId.New, Locations.Dallas, Locations.Chicago, 3.January(2000), 4.January(2000), A<VoyageId>(), CarrierMovementId.New),
                };

            // Act
            var isSatisfiedBy = sut.IsSatisfiedBy(transportLegs);
            var why = sut.WhyIsNotSatisfiedBy(transportLegs);

            // Assert
            isSatisfiedBy.Should().BeTrue();
            why.Should().HaveCount(0);
        }

        [Test]
        public void UnloadIsAfterLoad()
        {
            // Arrange
            var sut = new TransportLegsAreConnectedSpecification();
            var transportLegs = new[]
                {
                    new TransportLeg(TransportLegId.New, Locations.NewYork, Locations.Dallas, 1.January(2000), 3.January(2000), A<VoyageId>(), CarrierMovementId.New),
                    new TransportLeg(TransportLegId.New, Locations.Dallas, Locations.Chicago, 2.January(2000), 4.January(2000), A<VoyageId>(), CarrierMovementId.New),
                };

            // Act
            var isSatisfiedBy = sut.IsSatisfiedBy(transportLegs);
            var why = sut.WhyIsNotSatisfiedBy(transportLegs);

            // Assert
            isSatisfiedBy.Should().BeFalse();
            why.Should().HaveCount(1);
        }

        [Test]
        public void UnloadAndLoadLocationsAreDifferent()
        {
            // Arrange
            var sut = new TransportLegsAreConnectedSpecification();
            var transportLegs = new[]
                {
                    new TransportLeg(TransportLegId.New, Locations.NewYork, Locations.Dallas, 1.January(2000), 2.January(2000), A<VoyageId>(), CarrierMovementId.New),
                    new TransportLeg(TransportLegId.New, Locations.Shanghai, Locations.Chicago, 3.January(2000), 4.January(2000), A<VoyageId>(), CarrierMovementId.New),
                };

            // Act
            var isSatisfiedBy = sut.IsSatisfiedBy(transportLegs);
            var why = sut.WhyIsNotSatisfiedBy(transportLegs);

            // Assert
            isSatisfiedBy.Should().BeFalse();
            why.Should().HaveCount(1);
        }
    }
}