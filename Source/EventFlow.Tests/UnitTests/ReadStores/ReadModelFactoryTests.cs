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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Logs;
using EventFlow.ReadStores;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global

namespace EventFlow.Tests.UnitTests.ReadStores
{
    [Category(Categories.Unit)]
    public class ReadModelFactoryTests : Test
    {
        [Test]
        public async Task ReadModelFactoryCanBeConfigured()
        {
            const int expectedMagicNumberForReadModelA = 42;
            const int expectedMagicNumberForReadModelB = 84;
            const int expectedMagicNumberForReadModelC = 0;

            // Arrange
            using (var resolver = EventFlowOptions.New
                .RegisterServices(sr =>
                    {
                        sr.Register<IReadModelFactory<FancyReadModelA>>(r => new FancyReadModelFactory<FancyReadModelA>(expectedMagicNumberForReadModelA));
                        sr.Register<IReadModelFactory<FancyReadModelB>>(r => new FancyReadModelFactory<FancyReadModelB>(expectedMagicNumberForReadModelB));
                    })
                .CreateResolver())
            {
                // Act
                var readModelA = await resolver.Resolve<IReadModelFactory<FancyReadModelA>>().CreateAsync(A<string>(), CancellationToken.None);
                var readModelB = await resolver.Resolve<IReadModelFactory<FancyReadModelB>>().CreateAsync(A<string>(), CancellationToken.None);
                var readModelC = await resolver.Resolve<IReadModelFactory<FancyReadModelC>>().CreateAsync(A<string>(), CancellationToken.None);

                // Assert
                readModelA.MagicNumber.Should().Be(expectedMagicNumberForReadModelA);
                readModelB.MagicNumber.Should().Be(expectedMagicNumberForReadModelB);
                readModelC.MagicNumber.Should().Be(expectedMagicNumberForReadModelC);
            }
        }

        [Test]
        public void ThrowsExceptionForNoEmptyConstruuctors()
        {
            // Act + Assert
            var exception = Assert.Throws<TypeInitializationException>(() => new ReadModelFactory<ReadModelWithConstructorArguments>(Mock<ILog>()));
            
            // Assert
            // ReSharper disable once PossibleNullReferenceException
            exception.InnerException.Message.Should().Contain("doesn't have an empty constructor");
        }

        public class ReadModelWithConstructorArguments : IReadModel
        {
            // ReSharper disable once UnusedParameter.Local
            public ReadModelWithConstructorArguments(int magicNumber){ }
        }
        
        public interface IFancyReadModel : IReadModel
        {
            int MagicNumber { get; set; }
        }

        public class FancyReadModelA : IFancyReadModel
        {
            public int MagicNumber { get; set; }
        }

        public class FancyReadModelB : IFancyReadModel
        {
            public int MagicNumber { get; set; }
        }

        public class FancyReadModelC : IFancyReadModel
        {
            public int MagicNumber { get; set; }
        }

        public class FancyReadModelFactory<TReadModel> : IReadModelFactory<TReadModel>
            where TReadModel : IFancyReadModel, new()
        {
            private readonly int _startingMagicNumber;

            public FancyReadModelFactory(
                int startingMagicNumber)
            {
                _startingMagicNumber = startingMagicNumber;
            }

            public Task<TReadModel> CreateAsync(string id, CancellationToken cancellationToken)
            {
                return Task.FromResult(new TReadModel
                {
                    MagicNumber = _startingMagicNumber
                });
            }
        }
    }
}