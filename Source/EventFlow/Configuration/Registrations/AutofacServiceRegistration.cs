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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using EventFlow.Configuration.Decorators;
using EventFlow.Core;
using EventFlow.Extensions;

namespace EventFlow.Configuration.Registrations
{
    internal class AutofacServiceRegistration : IServiceRegistration
    {
        private readonly ContainerBuilder _containerBuilder;
        private readonly DecoratorService _decoratorService = new DecoratorService();

        public AutofacServiceRegistration() : this(null) { }
        public AutofacServiceRegistration(ContainerBuilder containerBuilder)
        {
            _containerBuilder = containerBuilder ?? new ContainerBuilder();

            _containerBuilder.RegisterType<AutofacStartable>().As<IStartable>();
            _containerBuilder.RegisterType<AutofacResolver>().As<IResolver>();
            _containerBuilder.RegisterType<AutofacScopeResolver>().As<IScopeResolver>();
            _containerBuilder.Register<IDecoratorService>(_ => _decoratorService).SingleInstance();
        }

        public void Register<TService, TImplementation>(
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
            where TImplementation : class, TService
            where TService : class
        {
            var registration = _containerBuilder.RegisterType<TImplementation>().AsSelf();
            if (lifetime == Lifetime.Singleton)
            {
                registration.SingleInstance();
            }

            var serviceRegistration = _containerBuilder
                .Register<TService>(c => c.Resolve<TImplementation>())
                .As<TService>()
                .OnActivating(args =>
                    {
                        var instance = _decoratorService.Decorate(args.Instance, new ResolverContext(new AutofacResolver(args.Context)));
                        args.ReplaceInstance(instance);
                    });
            if (keepDefault)
            {
                serviceRegistration.PreserveExistingDefaults();
            }
        }

        public void Register<TService>(
            Func<IResolverContext, TService> factory,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
            where TService : class
        {
            var registration = _containerBuilder
                .Register(cc => factory(new ResolverContext(new AutofacResolver(cc))))
                .OnActivating(args =>
                    {
                        var instance = _decoratorService.Decorate(args.Instance, new ResolverContext(new AutofacResolver(args.Context)));
                        args.ReplaceInstance(instance);
                    });
            if (lifetime == Lifetime.Singleton)
            {
                registration.SingleInstance();
            }
            if (keepDefault)
            {
                registration.PreserveExistingDefaults();
            }
        }

        public void Register(
            Type serviceType,
            Type implementationType,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
        {
            var registration = _containerBuilder
                .RegisterType(implementationType)
                .As(serviceType)
                .OnActivating(args =>
                    {
                        var instance = _decoratorService.Decorate(serviceType, args.Instance, new ResolverContext(new AutofacResolver(args.Context)));
                        args.ReplaceInstance(instance);
                    });
            if (lifetime == Lifetime.Singleton)
            {
                registration.SingleInstance();
            }
            if (keepDefault)
            {
                registration.PreserveExistingDefaults();
            }
        }

        public void RegisterType(
            Type serviceType,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
        {
            var registration = _containerBuilder
                .RegisterType(serviceType)
                .OnActivating(args =>
                    {
                        var instance = _decoratorService.Decorate(args.Instance, new ResolverContext(new AutofacResolver(args.Context)));
                        args.ReplaceInstance(instance);
                    });
            if (lifetime == Lifetime.Singleton)
            {
                registration.SingleInstance();
            }
            if (keepDefault)
            {
                registration.PreserveExistingDefaults();
            }
        }

        public void RegisterGeneric(
            Type serviceType,
            Type implementationType,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
        {
            var registration = _containerBuilder
                .RegisterGeneric(implementationType).As(serviceType);
            if (lifetime == Lifetime.Singleton)
            {
                registration.SingleInstance();
            }
        }

        public void RegisterIfNotRegistered<TService, TImplementation>(
            Lifetime lifetime = Lifetime.AlwaysUnique)
            where TService : class
            where TImplementation : class, TService
        {
            Register<TService, TImplementation>(lifetime, true);
        }

        public void Decorate<TService>(Func<IResolverContext, TService, TService> factory)
        {
            _decoratorService.AddDecorator(factory);
        }

        public IRootResolver CreateResolver(bool validateRegistrations)
        {
            var container = _containerBuilder.Build();
            var autofacRootResolver = new AutofacRootResolver(container);

            if (validateRegistrations)
            {
                autofacRootResolver.ValidateRegistrations();
            }

            return new AutofacRootResolver(container);
        }

        public class AutofacStartable : IStartable
        {
            private readonly IReadOnlyCollection<IBootstrap> _bootstraps;

            public AutofacStartable(
                IEnumerable<IBootstrap> bootstraps)
            {
                _bootstraps = OrderBootstraps(bootstraps);
            }

            public void Start()
            {
                using (var a = AsyncHelper.Wait)
                {
                    a.Run(StartAsync(CancellationToken.None));
                }
            }

            private Task StartAsync(CancellationToken cancellationToken)
            {
                return Task.WhenAll(_bootstraps.Select(b => b.BootAsync(cancellationToken)));
            }

            private static IReadOnlyCollection<IBootstrap> OrderBootstraps(IEnumerable<IBootstrap> bootstraps)
            {
                var list = bootstraps
                    .Select(b => new
                    {
                        Bootstrap = b,
                        AssemblyName = b.GetType().GetTypeInfo().Assembly.GetName().Name,
                    })
                    .ToList();
                var eventFlowBootstraps = list
                    .Where(a => a.AssemblyName.StartsWith("EventFlow"))
                    .OrderBy(a => a.AssemblyName)
                    .Select(a => a.Bootstrap);
                var otherBootstraps = list
                    .Where(a => !a.AssemblyName.StartsWith("EventFlow"))
                    .OrderBy(a => a.AssemblyName)
                    .Select(a => a.Bootstrap);
                return eventFlowBootstraps.Concat(otherBootstraps).ToList();
            }
        }
    }
}