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
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using TinyIoC;

namespace EventFlow.Configuration.Registrations
{
    internal class TinyIoCServiceRegistration : IServiceRegistration
    {
        private readonly TinyIoCContainer _container = new TinyIoCContainer();

        public TinyIoCServiceRegistration()
        {
            _container.Register<IResolver>((c, p) => new TinyIoCResolver(c));
        }

        public void Register<TService, TImplementation>(
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
            where TService : class
            where TImplementation : class, TService
        {
            // TODO: Keep default

            SetLifetime(_container.Register<TService, TImplementation>(), lifetime);
        }

        public void Register<TService>(
            Func<IResolverContext, TService> factory,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
            where TService : class
        {
            // TODO: Keep default
            // TODO: Lifetime

            _container.Register((c, p) => factory(new ResolverContext(new TinyIoCResolver(c))));
        }

        public void Register(
            Type serviceType,
            Type implementationType,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
        {
            // TODO: Keep default

            SetLifetime(_container.Register(serviceType, implementationType), lifetime);
        }

        public void RegisterType(
            Type serviceType,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
        {
            // TODO: Keep default

            SetLifetime(_container.Register(serviceType), lifetime);
        }

        public void RegisterGeneric(
            Type serviceType,
            Type implementationType,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
        {
            // TODO: Keep default

            SetLifetime(_container.Register(serviceType, implementationType), lifetime);
        }

        public void RegisterIfNotRegistered<TService, TImplementation>(
            Lifetime lifetime = Lifetime.AlwaysUnique)
            where TService : class
            where TImplementation : class, TService
        {
            if (_container.CanResolve<TService>())
            {
                return;
            }

            SetLifetime(_container.Register<TService, TImplementation>(), lifetime);
        }

        public void Decorate<TService>(Func<IResolverContext, TService, TService> factory)
        {
            // TODO: Create this...
        }

        public IRootResolver CreateResolver(bool validateRegistrations)
        {
            var bootstraps = GetBootstraps(_container);

            using (var a = AsyncHelper.Wait)
            {
                a.Run(BootAsync(bootstraps, CancellationToken.None));
            }

            return new TinyIoCResolver(_container);
        }

        private static IEnumerable<IBootstrap> GetBootstraps(TinyIoCContainer tinyIoCContainer)
        {
            var list = tinyIoCContainer.ResolveAll<IBootstrap>()
                .Select(b => new
                {
                    Bootstrap = b,
                    AssemblyName = b.GetType().Assembly.GetName().Name,
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

        private static Task BootAsync(IEnumerable<IBootstrap> bootstraps, CancellationToken cancellationToken)
        {
            return Task.WhenAll(bootstraps.Select(b => b.BootAsync(cancellationToken)));
        }

        private static TinyIoCContainer.RegisterOptions SetLifetime(TinyIoCContainer.RegisterOptions registerOptions, Lifetime lifetime)
        {
            switch (lifetime)
            {
                case Lifetime.AlwaysUnique:
                    registerOptions = registerOptions.AsMultiInstance();
                    break;
                case Lifetime.Singleton:
                    registerOptions = registerOptions.AsSingleton();
                    break;
                default:
                    throw new NotImplementedException($"Type '{lifetime}' not handled");
            }

            return registerOptions;
        }
    }
}