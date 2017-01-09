// The MIT License (MIT)
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

using System.Collections.Generic;
using System.Linq;
using EventFlow.Configuration;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Configuration.Registrations
{
    public abstract class SuiteServiceRegistrationTests : TestsFor<IServiceRegistration>
    {
        // ReSharper disable ClassNeverInstantiated.Local
        private interface IMagicInterface { }
        private class MagicClass : IMagicInterface { }
        private class MagicClassDecorator1 : IMagicInterface
        {
            public IMagicInterface Inner { get; }
            public MagicClassDecorator1(IMagicInterface magicInterface) { Inner = magicInterface; }
        }
        private class MagicClassDecorator2 : IMagicInterface
        {
            public IMagicInterface Inner { get; }
            public MagicClassDecorator2(IMagicInterface magicInterface) { Inner = magicInterface; }
        }

        private interface I
        {
        }

        private class A : I
        {
        }

        private class B : I
        {
        }

        private class C
        {
            public IEnumerable<I> Is { get; }

            public C(
                IEnumerable<I> nested)
            {
                Is = nested;
            }
        }

        // ReSharper enable ClassNeverInstantiated.Local

        [Test]
        public void ServiceViaFactory()
        {
            // Act
            Sut.Register<IMagicInterface>(r => new MagicClass());

            // Assert
            Assert_Service();
        }

        [Test]
        public void ServiceViaGeneric()
        {
            // Act
            Sut.Register<IMagicInterface, MagicClass>();

            // Assert
            Assert_Service();
        }

        [Test]
        public void ServiceViaType()
        {
            // Act
            Sut.Register(typeof(IMagicInterface), typeof(MagicClass));

            // Assert
            Assert_Service();
        }

        [Test]
        public void Enumerable()
        {
            // Arrange
            Sut.RegisterType(typeof(C));
            Sut.Register<I, A>();
            Sut.Register<I, B>();

            // Act
            var resolver = Sut.CreateResolver(false);

            // Assert
            var c = resolver.Resolve<C>();
            var nested = c.Is.ToList();
            nested[0].Should().BeOfType<B>();
            nested[1].Should().BeOfType<A>();
        }

        private void Assert_Service()
        {
            // Act
            var resolver = Sut.CreateResolver(true);
            var magicInterface = resolver.Resolve<IMagicInterface>();

            // Assert
            magicInterface.Should().NotBeNull();
            magicInterface.Should().BeAssignableTo<MagicClass>();
        }

        [Test]
        public void DecoratorViaFactory()
        {
            // Act
            Sut.Register<IMagicInterface>(r => new MagicClass());

            // Assert
            Assert_Decorator(Sut);
        }

        [Test]
        public void DecoratorViaGeneric()
        {
            // Act
            Sut.Register<IMagicInterface, MagicClass>();

            // Assert
            Assert_Decorator(Sut);
        }

        [Test]
        public void DecoratorViaType()
        {
            // Act
            Sut.Register(typeof(IMagicInterface), typeof(MagicClass));

            // Assert
            Assert_Decorator(Sut);
        }

        [Test]
        public void ResolverIsResolvable()
        {
            // Act
            var resolver = Sut.CreateResolver(true).Resolve<IResolver>();

            // Assert
            resolver.Should().NotBeNull();
            resolver.Should().BeAssignableTo<IResolver>();
        }

        public static void Assert_Decorator(IServiceRegistration serviceRegistration)
        {
            // The order should be like this (like unwrapping a present with the order of
            // wrapping paper applied)
            // 
            // Call      MagicClassDecorator2
            // Call      MagicClassDecorator1
            // Call      MagicClass
            // Return to MagicClassDecorator1
            // Return to MagicClassDecorator2

            // Arrange
            serviceRegistration.Decorate<IMagicInterface>((r, inner) => new MagicClassDecorator1(inner));
            serviceRegistration.Decorate<IMagicInterface>((r, inner) => new MagicClassDecorator2(inner));

            // Act
            var resolver = serviceRegistration.CreateResolver(true);
            var magic = resolver.Resolve<IMagicInterface>();

            // Assert
            magic.Should().BeAssignableTo<MagicClassDecorator2>();
            var magicClassDecorator2 = (MagicClassDecorator2) magic;
            magicClassDecorator2.Inner.Should().BeAssignableTo<MagicClassDecorator1>();
            var magicClassDecorator1 = (MagicClassDecorator1)magicClassDecorator2.Inner;
            magicClassDecorator1.Inner.Should().BeAssignableTo<MagicClass>();
        }
    }
}