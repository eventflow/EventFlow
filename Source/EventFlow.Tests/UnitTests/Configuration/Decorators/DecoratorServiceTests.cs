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

using EventFlow.Configuration;
using EventFlow.Configuration.Decorators;
using EventFlow.TestHelpers;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Configuration.Decorators
{
    [Category(Categories.Unit)]
    public class DecoratorServiceTests : TestsFor<DecoratorService>
    {
        // ReSharper disable ClassNeverInstantiated.Local
        // ReSharper disable MemberCanBePrivate.Local
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
        // ReSharper restore MemberCanBePrivate.Local
        // ReSharper enable ClassNeverInstantiated.Local

        private Mock<IResolverContext> _resolverContextMock;

        [SetUp]
        public void SetUp()
        {
            _resolverContextMock = new Mock<IResolverContext>();
        }

        [Test]
        public void NoDecoratorReturnsSame()
        {
            // Act
            var instance = Sut.Decorate<IMagicInterface>(new MagicClass(), _resolverContextMock.Object);

            // Assert
            instance.Should().NotBeNull();
            instance.Should().BeAssignableTo<MagicClass>();
        }

        [Test]
        public void WithTwoDecoratorsWithGeneric()
        {
            // Arrange
            Sut.AddDecorator<IMagicInterface>((r, s) => new MagicClassDecorator1(s));
            Sut.AddDecorator<IMagicInterface>((r, s) => new MagicClassDecorator2(s));

            // Act
            var instance = Sut.Decorate<IMagicInterface>(new MagicClass(), _resolverContextMock.Object);

            // Assert
            instance.Should().BeAssignableTo<MagicClassDecorator2>();
            var magicClassDecorator2 = (MagicClassDecorator2)instance;
            magicClassDecorator2.Inner.Should().BeAssignableTo<MagicClassDecorator1>();
            var magicClassDecorator1 = (MagicClassDecorator1)magicClassDecorator2.Inner;
            magicClassDecorator1.Inner.Should().BeAssignableTo<MagicClass>();
        }

        [Test]
        public void WithTwoDecoratorsWithTyped()
        {
            // Arrange
            Sut.AddDecorator<IMagicInterface>((r, s) => new MagicClassDecorator1(s));
            Sut.AddDecorator<IMagicInterface>((r, s) => new MagicClassDecorator2(s));

            // Act
            var instance = Sut.Decorate(typeof(IMagicInterface), new MagicClass(), _resolverContextMock.Object);

            // Assert
            instance.Should().BeAssignableTo<MagicClassDecorator2>();
            var magicClassDecorator2 = (MagicClassDecorator2)instance;
            magicClassDecorator2.Inner.Should().BeAssignableTo<MagicClassDecorator1>();
            var magicClassDecorator1 = (MagicClassDecorator1)magicClassDecorator2.Inner;
            magicClassDecorator1.Inner.Should().BeAssignableTo<MagicClass>();
        }
    }
}