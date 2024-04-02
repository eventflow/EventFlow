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

using System;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.ReadStores;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;
using EventFlow.Extensions;
using Microsoft.Extensions.DependencyInjection;

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
            using (var resolver = EventFlowOptions.New()
                .RegisterServices(sr =>
                    {
                        sr.AddTransient<IReadModelFactory<FancyReadModelA>>(r => new FancyReadModelFactory<FancyReadModelA>(expectedMagicNumberForReadModelA));
                        sr.AddTransient<IReadModelFactory<FancyReadModelB>>(r => new FancyReadModelFactory<FancyReadModelB>(expectedMagicNumberForReadModelB));
                    })
                .ServiceCollection.BuildServiceProvider())
            {
                // Act
                var readModelA = await resolver.GetRequiredService<IReadModelFactory<FancyReadModelA>>().CreateAsync(A<string>(), CancellationToken.None);
                var readModelB = await resolver.GetRequiredService<IReadModelFactory<FancyReadModelB>>().CreateAsync(A<string>(), CancellationToken.None);
                var readModelC = await resolver.GetRequiredService<IReadModelFactory<FancyReadModelC>>().CreateAsync(A<string>(), CancellationToken.None);

                // Assert
                readModelA.MagicNumber.Should().Be(expectedMagicNumberForReadModelA);
                readModelB.MagicNumber.Should().Be(expectedMagicNumberForReadModelB);
                readModelC.MagicNumber.Should().Be(expectedMagicNumberForReadModelC);
            }
        }

        [Test]
        public void ThrowsExceptionForNoEmptyConstructors()
        {
            // Act + Assert
            var exception = Assert.Throws<TypeInitializationException>(() => new ReadModelFactory<ReadModelWithConstructorArguments>(
                Logger<ReadModelFactory<ReadModelWithConstructorArguments>>()));
            
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