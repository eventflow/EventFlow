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
using System.Collections.Concurrent;
using System.Collections.Generic;
using EventFlow.Configuration;
using EventFlow.Configuration.Bootstraps;
using EventFlow.Configuration.Decorators;
using EventFlow.Core.IoC.Factories;
using EventFlow.Extensions;

namespace EventFlow.Core.IoC
{
    internal class EventFlowIoCServiceRegistration : IServiceRegistration
    {
        private readonly object _syncRoot = new object();
        private readonly Dictionary<Type, List<Registration>> _registrations = new Dictionary<Type, List<Registration>>();
        private readonly DecoratorService _decoratorService = new DecoratorService();

        public EventFlowIoCServiceRegistration()
        {
            Register<IBootstrapper, Bootstrapper>(Lifetime.Singleton);
        }

        public void Register<TService, TImplementation>(
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
            where TService : class
            where TImplementation : class, TService
        {
            Register(
                typeof(TService),
                new ConstructorFactory(typeof(TImplementation)),
                lifetime,
                keepDefault);
        }

        public void Register<TService>(
            Func<IResolverContext, TService> factory,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
            where TService : class
        {
            Register(
                typeof(TService),
                new LambdaFactory<TService>(factory),
                lifetime,
                keepDefault);
        }

        public void Register(
            Type serviceType,
            Type implementationType,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
        {
            Register(
                serviceType,
                new ConstructorFactory(implementationType),
                lifetime,
                keepDefault);
        }

        public void RegisterType(
            Type serviceType,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
        {
            Register(
                serviceType,
                new ConstructorFactory(serviceType),
                lifetime,
                keepDefault);
        }

        public void RegisterGeneric(
            Type serviceType,
            Type implementationType,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
        {
            Register(
                serviceType,
                new GenericFactory(implementationType),
                lifetime,
                keepDefault);
        }

        public void RegisterIfNotRegistered<TService, TImplementation>(
            Lifetime lifetime = Lifetime.AlwaysUnique)
            where TService : class
            where TImplementation : class, TService
        {
            Register<TService, TImplementation>(lifetime, true);
        }

        public void Decorate<TService>(
            Func<IResolverContext, TService, TService> factory)
        {
            _decoratorService.AddDecorator(factory);
        }

        public IRootResolver CreateResolver(bool validateRegistrations)
        {
            var resolver = new EventFlowIoCResolver(
                new ConcurrentDictionary<int, object>(), 
                _registrations,
                true);

            if (validateRegistrations)
            {
                resolver.ValidateRegistrations();
            }

            var bootstrapper = resolver.Resolve<IBootstrapper>();

#pragma warning disable 618
            // TODO: Figure out bootstrapping alternative for 1.0
            bootstrapper.Start();
#pragma warning restore 618

            return resolver;
        }

        private void Register(
            Type serviceType,
            IFactory factory,
            Lifetime lifetime,
            bool keepDefault)
        {
            lock (_syncRoot)
            {
                if (_registrations.TryGetValue(serviceType, out var registrations))
                {
                    if (keepDefault)
                    {
                        return;
                    }
                }
                else
                {
                    registrations = new List<Registration>();
                    _registrations.Add(serviceType, registrations);
                }

                var registration = new Registration(
                    serviceType,
                    lifetime,
                    factory,
                    _decoratorService);
                registrations.Insert(0, registration);
            }
        }
    }
}