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
// 

using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using EventFlow.Aggregates;
using EventFlow.Autofac.Extensions;
using EventFlow.Configuration;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Autofac.Tests.UnitTests.Aggregates
{
    [TestFixture]
    [Category(Categories.Integration)]
    public class AutofacAggregateFactoryTests
    {
        [Test]
        public async Task CreatesNewAggregateWithIdParameter()
        {
            // Arrange
            using (var resolver = EventFlowOptions.New
                .UseAutofacContainerBuilder()
                .UseAutofacAggregateRootFactory()
                .AddAggregateRoots(typeof(AutofacAggregateFactoryTests).GetTypeInfo().Assembly)
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
        public async Task ExternalContainerBuild()
        {
            // Arrange
            var containerBuilder = new ContainerBuilder();
            EventFlowOptions.New
                .UseAutofacContainerBuilder(containerBuilder)
                .UseAutofacAggregateRootFactory()
                .AddAggregateRoots(typeof (AutofacAggregateFactoryTests).GetTypeInfo().Assembly);

            using (var container = containerBuilder.Build())
            using (var lifetimeScope = container.BeginLifetimeScope())
            {
                var id = ThingyId.New;
                var sut = lifetimeScope.Resolve<IAggregateFactory>();

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
                .UseAutofacContainerBuilder()
                .UseAutofacAggregateRootFactory()
                .AddAggregateRoots(typeof(AutofacAggregateFactoryTests).GetTypeInfo().Assembly)
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
                .UseAutofacContainerBuilder()
                .UseAutofacAggregateRootFactory()
                .AddAggregateRoots(typeof(AutofacAggregateFactoryTests).Assembly)
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
