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

using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.TestHelpers;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Aggregates
{
    [Category(Categories.Unit)]
    public class AggregateFactoryTests : TestsFor<AggregateFactory>
    {
        private Mock<IResolver> _resolverMock;

        [SetUp]
        public void SetUp()
        {
            _resolverMock = InjectMock<IResolver>();
        }

        [Test]
        public async Task CanCreateIdOnlyAggregateRootAsync()
        {
            // Arrange
            var aggregateId = AggregateId.New;

            // Act
            var idOnlyAggregateRoot = await Sut.CreateNewAggregateAsync<IdOnlyAggregateRoot, AggregateId>(aggregateId).ConfigureAwait(false);

            // Assert
            idOnlyAggregateRoot.Should().NotBeNull();
            idOnlyAggregateRoot.Id.Should().Be(aggregateId);
        }

        [Test]
        public async Task CanCreateAggregateWithServices()
        {
            // Arrange
            var aggregateId = AggregateId.New;
            var serviceMock = new Mock<IService>();
            Arrange_Resolver(serviceMock.Object);

            // Act
            var aggregateWithServices = await Sut.CreateNewAggregateAsync<AggregateWithServices, AggregateId>(aggregateId).ConfigureAwait(false);

            // Assert
            aggregateWithServices.Should().NotBeNull();
            aggregateWithServices.Id.Should().Be(aggregateId);
            aggregateWithServices.Service.Should().BeSameAs(serviceMock.Object);

        }

        private void Arrange_Resolver<T>(T implementation)
        {
            _resolverMock
                .Setup(r => r.Resolve(typeof(T)))
                .Returns(implementation);
            _resolverMock
                .Setup(r => r.Resolve<T>())
                .Returns(implementation);
        }

        // ReSharper disable ClassNeverInstantiated.Local
        public interface IService
        {
        }

        private class AggregateId : Identity<AggregateId>
        {
            public AggregateId(string value) : base(value)
            {
            }
        }

        private class IdOnlyAggregateRoot : AggregateRoot<IdOnlyAggregateRoot, AggregateId>
        {
            public IdOnlyAggregateRoot(AggregateId id)
                : base(id)
            {
            }
        }

        private class AggregateWithServices : AggregateRoot<AggregateWithServices, AggregateId>
        {
            public IService Service { get; }

            public AggregateWithServices(
                AggregateId id,
                IService service)
                : base(id)
            {
                Service = service;
            }
        }
        // ReSharper restore ClassNeverInstantiated.Local
    }
}