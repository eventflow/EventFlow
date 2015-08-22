// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen, Jaco Coetzee
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

using Autofac;
using EventFlow.Aggregates;
using EventFlow.Autofac.Extensions;
using EventFlow.Configuration;
using EventFlow.TestHelpers.Aggregates.Test;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Autofac.Tests.UnitTests.Aggregates
{
    [TestFixture]
    public class AggregateFactoryTests
    {
        [Test]
        public async void AutofacAggregateFactoryResolvesConstructorParameters()
        {
            // Arrange
            var containerBuilder = new ContainerBuilder();
            using (var resolver = EventFlowOptions.New
                .UseAutofacContainerBuilder(containerBuilder)
                .UseAutofacAggregateFactory()
                .RegisterServices(f => f.RegisterType(typeof(TestAggregate)))
                .RegisterServices(f => f.RegisterType(typeof(TestAggregateWithResolver)))
                .RegisterServices(f => f.RegisterType(typeof(TestAggregateWithPinger)))
                .RegisterServices(f => f.RegisterType(typeof(Pinger)))
                .CreateResolver(false))
            {
                // Arrange
                var id = TestId.New;
                var sut = resolver.Resolve<IAggregateFactory>();

                // Act
                var a = await sut.CreateNewAggregateAsync<TestAggregate, TestId>(id);
                var ar = await sut.CreateNewAggregateAsync<TestAggregateWithResolver, TestId>(id);
                var ap = await sut.CreateNewAggregateAsync<TestAggregateWithPinger, TestId>(id);

                // Assert
                a.Id.Should().Be(id);
                ar.Resolver.Should().BeAssignableTo<IResolver>();
                ap.Pinger.Should().BeOfType<Pinger>();
            }
        }

        public class Pinger
        {
            public void Ping()
            {
                // void
            }
        }

        public class TestAggregate : AggregateRoot<TestAggregate, TestId>
        {
            public TestAggregate(TestId id)
                : base(id)
            {
            }
        }

        public class TestAggregateWithPinger : AggregateRoot<TestAggregateWithPinger, TestId>
        {
            public TestAggregateWithPinger(TestId id, Pinger pinger)
                : base(id)
            {
                Pinger = pinger;
            }

            public Pinger Pinger { get; }
        }

        public class TestAggregateWithResolver : AggregateRoot<TestAggregateWithResolver, TestId>
        {
            public TestAggregateWithResolver(TestId id, IResolver resolver)
                : base(id)
            {
                Resolver = resolver;
            }

            public IResolver Resolver { get; }
        }
    }
}
