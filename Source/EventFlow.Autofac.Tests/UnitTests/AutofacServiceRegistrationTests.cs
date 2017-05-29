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

using EventFlow.Autofac.Registrations;
using EventFlow.Configuration;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Suites;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventFlow.Autofac.Tests.UnitTests
{
    [Category(Categories.Unit)]
    public class AutofacServiceRegistrationTests : TestSuiteForServiceRegistration
    {
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

        protected override IServiceRegistration CreateSut()
        {
            return new AutofacServiceRegistration();
        }
    }
}