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

using System;
using System.Collections.Generic;
using System.Linq;
using EventFlow.Extensions;
using EventFlow.Queries;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Aggregates.Queries;
using EventFlow.Tests.IntegrationTests.ReadStores.QueryHandlers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Extensions
{

    [TestFixture]
    [Category(Categories.Unit)]
    public class EventFlowOptionsQueryExtensionsTests
    {
        public Mock<IEventFlowOptions> Options { get; private set; }
        public ServiceCollection ServiceCollection { get; private set; }

        [SetUp]
        public void Setup()
        {
            Options = new Mock<IEventFlowOptions>();
            ServiceCollection = new ServiceCollection();
            Options.Setup(x => x.ServiceCollection).Returns(ServiceCollection);
            Options.Setup(x => x.AddQueries(It.IsAny<IEnumerable<Type>>()));
        }

        [DatapointSource]
        public Type[] QueryHandlersInTestHelperAssembly = new[] {
            typeof(IQueryHandler<ThingyGetQuery, Thingy>),
            typeof(IQueryHandler<ThingyGetMessagesQuery, IReadOnlyCollection<ThingyMessage>>)
        };

        [Theory]
        public void AddQueryHandlers_FromAssembly_AddsAllQueryHandlersFromAssemblyToCollection(Type typeInAssembly)
        {
            Options.Object.AddQueryHandlers(typeof(InMemoryThingyGetMessagesQueryHandler).Assembly);

            var queryHandlerDescriptor = ServiceCollection
                .FirstOrDefault(descriptor => descriptor.ServiceType == typeInAssembly);

            queryHandlerDescriptor.Should().NotBeNull();
        }

        [Test]
        public void AddQueryHandlers_FromAssemblyWithPredicate_AddsAllQueryHandlersThatPassPredicateFromAssemblyToCollection()
        {
            var excludedqueryHandlerType = typeof(InMemoryThingyGetMessagesQueryHandler);

            Options.Object.AddQueryHandlers(typeof(InMemoryThingyGetMessagesQueryHandler).Assembly,
                type => type != excludedqueryHandlerType);

            var excludedQueryHandlerDescriptor = ServiceCollection
                .FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IQueryHandler<ThingyGetMessagesQuery, IReadOnlyCollection<ThingyMessage>>));
            excludedQueryHandlerDescriptor.Should().BeNull();
        }

        [Test]
        public void AddQueryHandlers_FromEnumerable_TypeDoesNotImplementIQueryHandler_ThrowsArgumentException()
        {
            var queryHandlersToAdd = new List<Type> {
                typeof(ThingyAddMessageCommandHandler)
            };

            Action action = () => Options.Object.AddQueryHandlers(queryHandlersToAdd);

            action.Should().Throw<ArgumentException>().And.Message.Should().Contain($"{typeof(IQueryHandler<,>).PrettyPrint()}");

        }

        [Test]
        public void AddQueryHandlers_FromEnumerable_AddsQueryHandlerToServiceCollection()
        {
            var queryHandlerType = typeof(InMemoryThingyGetMessagesQueryHandler);
            var queryHandlersToAdd = new List<Type> {
                queryHandlerType
            };

            Options.Object.AddQueryHandlers(queryHandlersToAdd);

            var queryHandlerDescriptor = ServiceCollection
                .FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IQueryHandler<ThingyGetMessagesQuery, IReadOnlyCollection<ThingyMessage>>));

            queryHandlerDescriptor.Should().NotBeNull();
        }

        [Test]
        public void AddQueryHandlers_FromEnumerable_AddsHandledQueriesToEventFlowOptions()
        {
            var queryHandlerType = typeof(InMemoryThingyGetMessagesQueryHandler);
            var queryHandlersToAdd = new List<Type> {
                queryHandlerType
            };

            Options.Object.AddQueryHandlers(queryHandlersToAdd);

            Options.Verify(x => x.AddQueries(It.Is<IEnumerable<Type>>(queries => queries.Contains(typeof(ThingyGetMessagesQuery)))));
        }

        [Test]
        public void AddQueryHandler_AddsHandlerToServiceCollection()
        {
            Options.Object.AddQueryHandler<InMemoryThingyGetMessagesQueryHandler, ThingyGetMessagesQuery, IReadOnlyCollection<ThingyMessage>>();


            var queryHandlerDescriptor = ServiceCollection
                .FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IQueryHandler<ThingyGetMessagesQuery, IReadOnlyCollection<ThingyMessage>>));

            queryHandlerDescriptor.Should().NotBeNull();
        }

        [Test]
        public void AddQueryHandler_AddsHandledQueryToEventFlowOptions()
        {
            Options.Object.AddQueryHandler<InMemoryThingyGetMessagesQueryHandler, ThingyGetMessagesQuery, IReadOnlyCollection<ThingyMessage>>();

            Options.Verify(x => x.AddQueries(It.Is<IEnumerable<Type>>(queries => queries.Contains(typeof(ThingyGetMessagesQuery)))));

        }
    }
}
