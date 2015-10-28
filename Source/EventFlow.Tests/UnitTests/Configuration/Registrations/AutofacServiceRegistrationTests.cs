// The MIT License (MIT)
// 
// Copyright (c) 2015 Rasmus Mikkelsen
// Copyright (c) 2015 eBay Software Foundation
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
using EventFlow.Configuration.Registrations;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Configuration.Registrations
{
    public class AutofacServiceRegistrationTests : Test
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
        // ReSharper enable ClassNeverInstantiated.Local

        private AutofacServiceRegistration _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = new AutofacServiceRegistration();
        }

        [Test]
        public void ServiceViaFactory()
        {
            // Act
            _sut.Register<IMagicInterface>(r => new MagicClass());

            // Assert
            Assert_Service();
        }

        [Test]
        public void ServiceViaGeneric()
        {
            // Act
            _sut.Register<IMagicInterface, MagicClass>();

            // Assert
            Assert_Service();
        }

        [Test]
        public void ServiceViaType()
        {
            // Act
            _sut.Register(typeof(IMagicInterface), typeof(MagicClass));

            // Assert
            Assert_Service();
        }

        public void Assert_Service()
        {
            // Act
            var resolver = _sut.CreateResolver(true);
            var magicInterface = resolver.Resolve<IMagicInterface>();

            // Assert
            magicInterface.Should().NotBeNull();
            magicInterface.Should().BeAssignableTo<MagicClass>();
        }

        [Test]
        public void DecoratorViaFactory()
        {
            // Act
            _sut.Register<IMagicInterface>(r => new MagicClass());

            // Assert
            Assert_Decorator();
        }

        [Test]
        public void DecoratorViaGeneric()
        {
            // Act
            _sut.Register<IMagicInterface, MagicClass>();

            // Assert
            Assert_Decorator();
        }

        [Test]
        public void DecoratorViaType()
        {
            // Act
            _sut.Register(typeof(IMagicInterface), typeof(MagicClass));

            // Assert
            Assert_Decorator();
        }

        public void Assert_Decorator()
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
            _sut.Decorate<IMagicInterface>((r, inner) => new MagicClassDecorator1(inner));
            _sut.Decorate<IMagicInterface>((r, inner) => new MagicClassDecorator2(inner));

            // Act
            var resolver = _sut.CreateResolver(true);
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
