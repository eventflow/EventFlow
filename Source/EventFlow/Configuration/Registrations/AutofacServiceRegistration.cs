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
using EventFlow.Configuration.Registrations.Resolvers;
using EventFlow.Extensions;

namespace EventFlow.Configuration.Registrations
{
    internal class AutofacServiceRegistration : IServiceRegistration
    {
        private readonly ContainerBuilder _containerBuilder;
        private readonly List<Registration> _registrations = new List<Registration>();

        public AutofacServiceRegistration() : this(null) { }
        public AutofacServiceRegistration(ContainerBuilder containerBuilder)
        {
            _containerBuilder = containerBuilder ?? new ContainerBuilder();
        }

        public void Register<TService, TImplementation>(Lifetime lifetime = Lifetime.AlwaysUnique)
            where TImplementation : class, TService
            where TService : class
        {
            _registrations.Add(new Registration<TService, TImplementation>(lifetime));
        }

        public void Register<TService>(Func<IResolver, TService> factory, Lifetime lifetime = Lifetime.AlwaysUnique)
            where TService : class
        {
            _registrations.Add(new Registration<TService>(factory, lifetime));
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
            foreach (var reg in _registrations)
            {
                reg.Configure(_containerBuilder);
            }

            var container = _containerBuilder.Build();

            if (validateRegistrations)
            {
                container.ValidateRegistrations();
            }

            return new AutofacRootResolver(container);
        }
    }
}
