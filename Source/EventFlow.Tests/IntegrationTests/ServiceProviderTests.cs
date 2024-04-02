// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.Queries;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EventFlow.Tests.IntegrationTests
{
    [Category(Categories.Integration)]
    public class ServiceProviderTests
    {
        public class Service { }

        public class ServiceDependentAggregate : AggregateRoot<ServiceDependentAggregate, ThingyId>
        {
            public Service Service { get; }

            public ServiceDependentAggregate(ThingyId id, Service service) : base(id)
            {
                Service = service;
            }
        }

        [Test]
        public async Task ResolverAggregatesFactoryCanResolve()
        {
            using (var serviceProvider = EventFlowOptions.New()
                .RegisterServices(sr => sr.AddTransient(typeof(Service)))
                .ServiceCollection.BuildServiceProvider())
            {
                // Arrange
                var aggregateFactory = serviceProvider.GetRequiredService<IAggregateFactory>();

                // Act
                var serviceDependentAggregate = await aggregateFactory.CreateNewAggregateAsync<ServiceDependentAggregate, ThingyId>(ThingyId.New).ConfigureAwait(false);

                // Assert
                serviceDependentAggregate.Service.Should()
                    .NotBeNull()
                    .And
                    .BeOfType<Service>();
            }
        }

        [Test]
        public void RegistrationDoesntCauseStackOverflow()
        {
            using (var serviceProvider = EventFlowOptions.New()
                .AddDefaults(EventFlowTestHelpers.Assembly)
                .RegisterServices(s =>
                {
                    s.AddScoped<IScopedContext, ScopedContext>();
                })
                .ServiceCollection.BuildServiceProvider())
            {
                serviceProvider.GetRequiredService<ICommandHandler<ThingyAggregate, ThingyId, IExecutionResult, ThingyAddMessageCommand>>();
            }
        }
    }
}