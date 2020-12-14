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
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.IntegrationTests.Aggregates
{
    [TestFixture]
    [Category(Categories.Integration)]
    public class AggregateFactoryTests
    {
        [Test]
        public async Task CreatesNewAggregateWithIdParameter()
        {
            // Arrange
            using (var resolver = EventFlowOptions.New
                .CreateResolver())
            {
                var id = ThingyId.New;
                var sut = resolver.Resolve<IAggregateFactory>();

                // Act
                var aggregateWithIdParameter = await sut.CreateNewAggregateAsync<TestAggregate, ThingyId>(id).ConfigureAwait(false);

                // Assert
                aggregateWithIdParameter.Id.Should().Be(id);
            }
        }

        [Test]
        public async Task CreatesNewAggregateWithIdAndInterfaceParameters()
        {
            // Arrange
            using (var resolver = EventFlowOptions.New
                .CreateResolver())
            {
                var sut = resolver.Resolve<IAggregateFactory>();

                // Act
                var aggregateWithIdAndInterfaceParameters = await sut.CreateNewAggregateAsync<TestAggregateWithResolver, ThingyId>(ThingyId.New).ConfigureAwait(false);

                // Assert
                aggregateWithIdAndInterfaceParameters.Resolver.Should().BeAssignableTo<IResolver>();
            }
        }

        [Test]
        public async Task CreatesNewAggregateWithIdAndTypeParameters()
        {
            // Arrange
            using (var resolver = EventFlowOptions.New
                .RegisterServices(f => f.RegisterType(typeof(Pinger)))
                .CreateResolver())
            {
                var sut = resolver.Resolve<IAggregateFactory>();

                // Act
                var aggregateWithIdAndTypeParameters = await sut.CreateNewAggregateAsync<TestAggregateWithPinger, ThingyId>(ThingyId.New).ConfigureAwait(false);

                // Assert
                aggregateWithIdAndTypeParameters.Pinger.Should().BeOfType<Pinger>();
            }
        }

        public class Pinger
        {
        }

        public class TestAggregate : AggregateRoot<TestAggregate, ThingyId>
        {
            public TestAggregate(ThingyId id)
                : base(id)
            {
            }
        }

        public class TestAggregateWithPinger : AggregateRoot<TestAggregateWithPinger, ThingyId>
        {
            public TestAggregateWithPinger(ThingyId id, Pinger pinger)
                : base(id)
            {
                Pinger = pinger;
            }

            public Pinger Pinger { get; }
        }

        public class TestAggregateWithResolver : AggregateRoot<TestAggregateWithResolver, ThingyId>
        {
            public TestAggregateWithResolver(ThingyId id, IResolver resolver)
                : base(id)
            {
                Resolver = resolver;
            }

            public IResolver Resolver { get; }
        }
    }
}