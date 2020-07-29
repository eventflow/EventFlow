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
using EventFlow.Configuration;
using EventFlow.TestHelpers;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Configuration
{
    [Category(Categories.Unit)]
    public class ModuleRegistrationTests : TestsFor<ModuleRegistration>
    {
        [Test]
        public void RegisterInvokesRegister()
        {
            // Arrange
            var moduleA = new Mock<IModuleA>();

            // Act
            Sut.Register(moduleA.Object);

            // Assert
            moduleA.Verify(m => m.Register(It.IsAny<IEventFlowOptions>()), Times.Once);
        }

        [Test]
        public void CannotRegisterSameModuleTwice()
        {
            // Arrange
            var moduleA = new Mock<IModuleA>();
            var anotherModuleA = new Mock<IModuleA>();

            // Act
            Sut.Register(moduleA.Object);
            Assert.Throws<ArgumentException>(() => Sut.Register(anotherModuleA.Object));
        }

        [Test]
        public void RegisterCanRegisterMultipleModules()
        {
            // Arrange
            var moduleA = new Mock<IModuleA>();
            var moduleB = new Mock<IModuleB>();

            // Act
            Sut.Register(moduleA.Object);
            Sut.Register(moduleB.Object);

            // Assert
            Sut.GetModule<IModuleA>().Should().Be(moduleA.Object);
            Sut.GetModule<IModuleB>().Should().Be(moduleB.Object);
        }

        [Test]
        public void GetModuleThrowsExceptionForUnknownModule()
        {
            // Arrange
            var aCompletlyDifferentModule = new Mock<IModuleA>();
            Sut.Register(aCompletlyDifferentModule.Object);

            // Act
            Assert.Throws<ArgumentException>(() => Sut.GetModule<IModuleB>());
        }

        [Test]
        public void TryGetModuleReturnsFalseForUnknownModule()
        {
            // Arrange
            var aCompletlyDifferentModule = new Mock<IModuleA>();
            Sut.Register(aCompletlyDifferentModule.Object);
            IModuleB moduleB;

            // Act
            Sut.TryGetModule(out moduleB).Should().BeFalse();
        }

        [Test]
        public void TryGetModuleReturnsModuleAndTrueForKnownModule()
        {
            // Arrange
            var moduleA = new Mock<IModuleA>();
            Sut.Register(moduleA.Object);
            IModuleA fetchedModuleA;

            // Act
            Sut.TryGetModule(out fetchedModuleA).Should().BeTrue();

            // Assert
            fetchedModuleA.Should().Be(moduleA.Object);
        }

        public interface IModuleA : IModule
        {
        }

        public interface IModuleB : IModule
        {
        }
    }
}