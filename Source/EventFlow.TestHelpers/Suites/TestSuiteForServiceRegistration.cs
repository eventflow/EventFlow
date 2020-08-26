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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.Queries;
using EventFlow.Subscribers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.Queries;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventFlow.TestHelpers.Suites
{
    public abstract class TestSuiteForServiceRegistration : TestsFor<IServiceRegistration>
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

        public interface I : IDisposable
        {
        }

        private class A : I
        {
            public static object SyncRoot { get; } = new object();
            public static bool WasDisposed { get; set; }

            public void Dispose()
            {
                if (WasDisposed) throw new ObjectDisposedException("A was already disposed!");

                WasDisposed = true;
            }
        }

        private class B : I
        {
            public void Dispose()
            {
            }
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
        public void ServiceViaGenericNotFoundThrowsException()
        {
            var resolver = Sut.CreateResolver(false);
            Action callingResolve = () => resolver.Resolve<IMagicInterface>();
            callingResolve.Should().Throw<Exception>();
        }

        [Test]
        public void ServiceViaTypeNotFoundThrowsException()
        {
            var resolver = Sut.CreateResolver(false);
            Action callingResolve = () => resolver.Resolve(typeof(IMagicInterface));
            callingResolve.Should().Throw<Exception>();
        }

        [Test]
        public void EnumerableTypesAreResolved()
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
            nested.Should().Contain(x => x.GetType() == typeof(A));
            nested.Should().Contain(x => x.GetType() == typeof(B));
        }

        [Test]
        public void EmptyEnumerableTypesAreEmpty()
        {
            // Arrange
            Sut.RegisterType(typeof(C));

            // Act
            var resolver = Sut.CreateResolver(false);

            // Assert
            var c = resolver.Resolve<C>();
            c.Is.Should().BeEmpty();
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
        public void ResolvesScopeResolver()
        {
            // Act
            var resolver = Sut.CreateResolver(true);
            var scopeResolver = resolver.Resolve<IScopeResolver>();

            // Assert
            scopeResolver.Should().NotBeNull();
        }

        [Test]
        public void AlwaysUnique()
        {
            // Arrange
            Sut.Register<I, A>();

            // Act
            var resolver = Sut.CreateResolver(false);
            var i1 = resolver.Resolve<I>();
            var i2 = resolver.Resolve<I>();

            // Assert
            i1.Should().NotBeSameAs(i2);
        }

        [Test]
        public void Singletons()
        {
            // Arrange
            Sut.Register<I, A>(Lifetime.Singleton);

            // Act
            var resolver = Sut.CreateResolver(true);
            var i1 = resolver.Resolve<I>();
            var i2 = resolver.Resolve<I>();

            // Assert
            i1.Should().BeSameAs(i2);
        }

        [Test]
        public void ScopesDoesntDisposeSingletons()
        {
            lock (A.SyncRoot)
            {
                // Arrange
                Sut.Register<I, A>(Lifetime.Singleton);

                using (var resolver = Sut.CreateResolver(false))
                {
                    var i1 = resolver.Resolve<I>();

                    // Act
                    using (var scopeResolver = resolver.BeginScope())
                    {
                        var i2 = scopeResolver.Resolve<I>();
                        i2.Should().BeSameAs(i1);
                    }

                    // Assert
                    A.WasDisposed.Should().BeFalse();
                }

                A.WasDisposed.Should().BeTrue();
                A.WasDisposed = false;
            }
        }

        [Test]
        public void ResolvingScopeResolversCreatesScope()
        {
            lock (A.SyncRoot)
            {
                // Arrange
                Sut.Register<I, A>(Lifetime.Singleton);

                using (var resolver = Sut.CreateResolver(false))
                {
                    var i1 = resolver.Resolve<I>();

                    // Act
                    using (var scopeResolver = resolver.Resolve<IScopeResolver>())
                    {
                        var i2 = scopeResolver.Resolve<I>();
                        i2.Should().BeSameAs(i1);
                    }

                    // Assert
                    A.WasDisposed.Should().BeFalse();
                }

                A.WasDisposed.Should().BeTrue();
                A.WasDisposed = false;
            }
        }

        [Test]
        public void InvokesBootstraps()
        {
            // Arrange
            var bootstrapMock = new Mock<IBootstrap>();

            // Act
            Sut.Register(c => bootstrapMock.Object);
            Sut.CreateResolver(true);

            // Assert
            bootstrapMock.Verify(m => m.BootAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Test]
        public void DisposedIsInvoked()
        {
            // Arrange
            var iMock = new Mock<I>();
            Sut.Register(_ => iMock.Object, Lifetime.Singleton);

            // Act
            using (var resolver = Sut.CreateResolver(false))
            {
                resolver.Resolve<I>().Should().BeSameAs(iMock.Object);
                resolver.Resolve<I>().Should().BeSameAs(iMock.Object);
            }

            // Assert
            iMock.Verify(i => i.Dispose(), Times.Once());
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

        [Test]
        public void AbstractMetadataProviderIsNotRegistered()
        {
            // Arrange
            var sut = EventFlowOptions.New.UseServiceRegistration(Sut);

            // Act
            Action act = () => sut.AddMetadataProviders(new List<Type>
            {
                typeof(AbstractTestSubscriber)
            });

            // Assert
            act.Should().NotThrow<ArgumentException>();
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

        [Test]
        public void AbstractSubscriberIsNotRegistered()
        {
            // Arrange
            var sut = EventFlowOptions.New.UseServiceRegistration(Sut);

            // Act
            Action act = () => sut.AddSubscribers(new List<Type>
            {
                typeof(AbstractTestSubscriber)
            });

            // Assert
            act.Should().NotThrow<ArgumentException>();
        }

        public abstract class AbstractTestSubscriber :
            ISubscribeSynchronousTo<ThingyAggregate, ThingyId, ThingyPingEvent>
        {
            public abstract Task HandleAsync(
                IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent> domainEvent,
                CancellationToken cancellationToken);
        }

        public abstract class AbstractTestMetadataProvider : IMetadataProvider
        {
            public abstract IEnumerable<KeyValuePair<string, string>> ProvideMetadata
                <TAggregate, TIdentity>(TIdentity id, IAggregateEvent aggregateEvent, IMetadata metadata)
                where TAggregate : IAggregateRoot<TIdentity> where TIdentity : IIdentity;
        }

        [Test]
        public void AbstractCommandHandlerIsNotRegistered()
        {
            // Arrange
            var sut = EventFlowOptions.New.UseServiceRegistration(Sut);

            // Act
            Action act = () => sut.AddCommandHandlers(new List<Type>
            {
                typeof(AbstractTestCommandHandler)
            });

            // Assert
            act.Should().NotThrow<ArgumentException>();
        }

        public abstract class AbstractTestCommandHandler :
            CommandHandler<ThingyAggregate, ThingyId, ThingyPingCommand>
        {
        }

        [Test]
        public void AbstractEventUpgraderIsNotRegistered()
        {
            // Arrange
            var sut = EventFlowOptions.New.UseServiceRegistration(Sut);

            // Act
            Action act = () => sut.AddEventUpgraders(new List<Type>
            {
                typeof(AbstractTestEventUpgrader)
            });

            // Assert
            act.Should().NotThrow<ArgumentException>();
        }

        public abstract class AbstractTestEventUpgrader : IEventUpgrader<ThingyAggregate, ThingyId>
        {
            public abstract IEnumerable<IDomainEvent<ThingyAggregate, ThingyId>> Upgrade(
                IDomainEvent<ThingyAggregate, ThingyId> domainEvent);
        }

        [Test]
        public void AbstractQueryHandlerIsNotRegistered()
        {
            // Arrange
            var sut = EventFlowOptions.New.UseServiceRegistration(Sut);

            // Act
            Action act = () => sut.AddQueryHandlers(new List<Type>
            {
                typeof(AbstractTestQueryHandler)
            });

            // Assert
            act.Should().NotThrow<ArgumentException>();
        }
        
        [Test]
        public async Task AggregateFactoryWorks()
        {
            // Arrange
            Sut.Register<IAggregateFactory, AggregateFactory>();
            var resolver = Sut.CreateResolver(false);
            var aggregateFactory = resolver.Resolve<IAggregateFactory>();
            var customId = new CustomId(A<string>());

            // Act
            var customAggregate = await aggregateFactory.CreateNewAggregateAsync<CustomAggregate, CustomId>(customId).ConfigureAwait(false);

            // Assert
            customAggregate.Id.Value.Should().Be(customId.Value);
        }

        public abstract class AbstractTestQueryHandler : IQueryHandler<ThingyGetQuery, Thingy>
        {
            public abstract Task<Thingy> ExecuteQueryAsync(ThingyGetQuery query,
                CancellationToken cancellationToken);
        }

        public class CustomId : IIdentity
        {
            public CustomId(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        public class CustomAggregate : AggregateRoot<CustomAggregate, CustomId>
        {
            public CustomAggregate(CustomId id) : base(id)
            {
            }
        }
    }

    public abstract class AbstractTestAggregate : AggregateRoot<ThingyAggregate, ThingyId>,
    IEmit<ThingyDomainErrorAfterFirstEvent>
    {
        protected AbstractTestAggregate(ThingyId id) : base(id)
        {
        }

        public void Apply(ThingyDomainErrorAfterFirstEvent aggregateEvent)
        {
        }
    }

    public class LocalTestAggregate : AbstractTestAggregate
    {
        public LocalTestAggregate(ThingyId id) : base(id)
        {
        }
    }
}