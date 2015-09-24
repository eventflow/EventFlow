// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
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

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Core;
using EventFlow.Aggregates;

namespace EventFlow.Configuration.Registrations
{
    internal class AutofacServiceRegistration : IServiceRegistration
    {
        private readonly ContainerBuilder _containerBuilder;
        private readonly ResolverDecorator _resolverDecorator = new ResolverDecorator();

        public AutofacServiceRegistration() : this(null) { }
        public AutofacServiceRegistration(ContainerBuilder containerBuilder)
        {
            _containerBuilder = containerBuilder ?? new ContainerBuilder();
            _containerBuilder.RegisterType<AutofacStartable>().As<IStartable>();
            _containerBuilder.Register(c => new AutofacResolver(c.Resolve<IComponentContext>())).As<IResolver>();
            _containerBuilder.Register<IResolverDecorator>(_ => _resolverDecorator).SingleInstance();
        }

        public void Register<TService, TImplementation>(Lifetime lifetime = Lifetime.AlwaysUnique)
            where TImplementation : class, TService
            where TService : class
        {
            var registration = _containerBuilder.RegisterType<TImplementation>().AsSelf();
            if (lifetime == Lifetime.Singleton)
            {
                registration.SingleInstance();
            }

            _containerBuilder
                .Register<TService>(c => c.Resolve<TImplementation>())
                .As<TService>()
                .OnActivating(args =>
                    {
                        var instance = _resolverDecorator.Decorate(args.Instance, new ResolverContext(new AutofacResolver(args.Context)));
                        args.ReplaceInstance(instance);
                    });
        }

        public void Register<TService>(Func<IResolverContext, TService> factory, Lifetime lifetime = Lifetime.AlwaysUnique) where TService : class
        {
            var registration = _containerBuilder
                .Register(cc => factory(new ResolverContext(new AutofacResolver(cc))))
                .OnActivating(args =>
                    {
                        var instance = _resolverDecorator.Decorate(args.Instance, new ResolverContext(new AutofacResolver(args.Context)));
                        args.ReplaceInstance(instance);
                    });
            if (lifetime == Lifetime.Singleton)
            {
                registration.SingleInstance();
            }
        }

        public void Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.AlwaysUnique)
        {
            var registration = _containerBuilder
                .RegisterType(implementationType)
                .As(serviceType)
                .OnActivating(args =>
                    {
                        var instance = _resolverDecorator.Decorate(args.Instance, new ResolverContext(new AutofacResolver(args.Context)));
                        args.ReplaceInstance(instance);
                    });
            if (lifetime == Lifetime.Singleton)
            {
                registration.SingleInstance();
            }
        }

        public void RegisterType(Type serviceType, Lifetime lifetime = Lifetime.AlwaysUnique)
        {
            var registration = _containerBuilder
                .RegisterType(serviceType)
                .OnActivating(args =>
                    {
                        var instance = _resolverDecorator.Decorate(args.Instance, new ResolverContext(new AutofacResolver(args.Context)));
                        args.ReplaceInstance(instance);
                    });
            if (lifetime == Lifetime.Singleton)
            {
                registration.SingleInstance();
            }
        }

        public void RegisterGeneric(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.AlwaysUnique)
        {
            var registration = _containerBuilder
                .RegisterGeneric(implementationType).As(serviceType);
            if (lifetime == Lifetime.Singleton)
            {
                registration.SingleInstance();
            }
        }

        public void Decorate<TService>(Func<IResolverContext, TService, TService> factory)
        {
            _resolverDecorator.AddDecorator(factory);
        }

        public IRootResolver CreateResolver(bool validateRegistrations)
        {
            var container = _containerBuilder.Build();

            if (validateRegistrations)
            {
                ValidateRegistrations(container);
            }

            return new AutofacRootResolver(container);
        }

        private static void ValidateRegistrations(IComponentContext container)
        {
            var services = container.ComponentRegistry.Registrations
                .SelectMany(x => x.Services)
                .OfType<TypedService>()
                .Where(x => !x.ServiceType.Name.StartsWith("Autofac"))
                .Where(x => !x.ServiceType.IsClosedTypeOf(typeof(IAggregateRoot<>)))
                .ToList();
            var exceptions = new List<Exception>();
            foreach (var typedService in services)
            {
                try
                {
                    container.Resolve(typedService.ServiceType);
                }
                catch (DependencyResolutionException ex)
                {
                    exceptions.Add(ex);
                }
            }
            if (!exceptions.Any())
            {
                return;
            }

            var message = string.Join(", ", exceptions.Select(e => e.Message));
            throw new AggregateException(message, exceptions);
        }
    }
}