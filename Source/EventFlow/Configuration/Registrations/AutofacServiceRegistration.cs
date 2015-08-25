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
        private readonly List<AutofacRegistration> _registrations = new List<AutofacRegistration>();
        private readonly Dictionary<Type, List<AutofacDecorator>> _decorators = new Dictionary<Type, List<AutofacDecorator>>();

        public AutofacServiceRegistration() : this(null) { }
        public AutofacServiceRegistration(ContainerBuilder containerBuilder)
        {
            _containerBuilder = containerBuilder ?? new ContainerBuilder();
        }

        public void Register<TService, TImplementation>(Lifetime lifetime = Lifetime.AlwaysUnique)
            where TImplementation : class, TService
            where TService : class
        {
            _registrations.Add(new AutofacRegistration<TService, TImplementation>(lifetime));
        }

        public void Register<TService>(Func<IResolverContext, TService> factory, Lifetime lifetime = Lifetime.AlwaysUnique)
            where TService : class
        {
            _registrations.Add(new AutofacRegistration<TService>(factory, lifetime));
        }

        public void Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.AlwaysUnique)
        {
            _registrations.Add(new AutofacRegistration(serviceType, implementationType, lifetime));
        }

        public void RegisterType(Type serviceType, Lifetime lifetime = Lifetime.AlwaysUnique)
        {
            _registrations.Add(new AutofacRegistration(serviceType, serviceType, lifetime));
        }

        public void RegisterGeneric(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.AlwaysUnique)
        {
            _registrations.Add(new AutofacGenericRegistration(serviceType, implementationType, lifetime));
        }

        public void Decorate<TService>(Func<IResolverContext, TService, TService> factory)
        {
            var serviceType = typeof (TService);
            List<AutofacDecorator> decorators;

            if (!_decorators.TryGetValue(serviceType, out decorators))
            {
                decorators = new List<AutofacDecorator>();
                _decorators.Add(serviceType, decorators);
            }

            decorators.Add(new AutofacDecorator<TService>(factory));
        }

        public bool HasRegistrationFor<TService>()
            where TService : class
        {
            var serviceType = typeof (TService);
            return _registrations.Any(r => r.ServiceType == serviceType);
        }

        public IEnumerable<Type> GetRegisteredServices()
        {
            return _registrations.Select(r => r.ServiceType).Distinct();
        }

        public IRootResolver CreateResolver(bool validateRegistrations)
        {
            _containerBuilder.Register(c => new AutofacResolver(c.Resolve<IComponentContext>())).As<IResolver>();

            foreach (var registration in _registrations)
            {
                registration.Configure(_containerBuilder, _decorators.ContainsKey(registration.ServiceType));
            }

            foreach (var kv in _decorators)
            {
                foreach (var a in kv.Value.Select((d, i) => new { i = kv.Value.Count - i, d }))
                {
                    a.d.Configure(_containerBuilder, a.i, kv.Value.Count != a.i);
                }
            }

            var container = _containerBuilder.Build();

            if (validateRegistrations)
            {
                ValidateRegistrations(container);
            }

            return new AutofacRootResolver(container);
        }

        private static void ValidateRegistrations(IComponentContext container)
        {
            var services = container
                .ComponentRegistry
                .Registrations
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
