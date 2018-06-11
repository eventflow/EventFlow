// The MIT License (MIT)
// 
// Copyright (c) 2015-2018 Rasmus Mikkelsen
// Copyright (c) 2015-2018 eBay Software Foundation
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

using EventFlow.Configuration;
using EventFlow.ServiceProvider.Extensions;
using EventFlow.ServiceProvider.Registrations;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Suites;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace EventFlow.ServiceProvider.Tests.UnitTests
{
    [Category(Categories.Unit)]
    public class ServiceCollectionServiceRegistrationTests : TestSuiteForServiceRegistration
    {
        private ServiceCollection _serviceCollection;

        [Test]
        public void ValidateRegistrationsShouldDispose()
        {
            // Arrange
            var service = new Mock<I>();
            var createdCount = 0;
            Sut.Register(_ =>
            {
                createdCount++;
                return service.Object;
            });

            // Act and Assert
            using (var resolver = Sut.CreateResolver(true))
            {
                createdCount.Should().Be(1);
                service.Verify(m => m.Dispose(), Times.Once);

                var resolvedService = resolver.Resolve<I>();
                createdCount.Should().Be(2);
                resolvedService.Should().BeSameAs(service.Object);

                using (var scopedResolver = resolver.BeginScope())
                {
                    var nestedResolvedService = scopedResolver.Resolve<I>();
                    createdCount.Should().Be(3);
                    nestedResolvedService.Should().BeSameAs(service.Object);
                }

                service.Verify(m => m.Dispose(), Times.Exactly(2));
            }

            service.Verify(m => m.Dispose(), Times.Exactly(3));
        }

        [Test]
        public void AddEventFlowRegistersEventFlowInServiceCollection()
        {
            // Arrange
            var collection = new ServiceCollection();

            // Act
            var provider = collection.AddEventFlow(o => { }).BuildServiceProvider();

            // Assert
            provider.GetService<ICommandBus>().Should().BeOfType<CommandBus>();
        }

        [Test]
        public void CreateServiceProviderReturnsConfiguredServiceProvider()
        {
            // Arrange
            var options = EventFlowOptions.New.UseServiceCollection();

            // Act
            var provider = options.CreateServiceProvider();

            // Assert
            provider.GetService<ICommandBus>().Should().BeOfType<CommandBus>();
        }

        protected override IServiceRegistration CreateSut()
        {
            _serviceCollection = new ServiceCollection();
            return new ServiceCollectionServiceRegistration(_serviceCollection);
        }
    }
}
