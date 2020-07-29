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
using EventFlow.Configuration.Bootstraps;
using EventFlow.Configuration.Decorators;
using EventFlow.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventFlow.DependencyInjection.Registrations
{
    public class ServiceCollectionServiceRegistration : IServiceRegistration
    {
        private readonly IServiceCollection _collection;
        private readonly DecoratorService _decoratorService = new DecoratorService();

        public ServiceCollectionServiceRegistration(IServiceCollection collection = null)
        {
            _collection = collection ?? new ServiceCollection();

            Register(c => c.Resolver);
            Register<IScopeResolver>(c =>
                new ServiceProviderScopeResolver(((ServiceProviderResolver) c.Resolver).ServiceProvider.CreateScope(),
                    _collection));
            Register<IDecoratorService>(c => _decoratorService, Lifetime.Singleton);
            Register<IBootstrapper, Bootstrapper>(Lifetime.Singleton);
        }

        public void Register<TService, TImplementation>(Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false) where TService : class where TImplementation : class, TService
        {
            var serviceType = typeof(TService);
            RegisterFactory(serviceType, lifetime, keepDefault, provider =>
            {
                var context = GetContext(provider);
                var implementation = ActivatorUtilities.CreateInstance<TImplementation>(provider);
                return _decoratorService.Decorate(serviceType, implementation, context);
            });
        }

        public void Register<TService>(Func<IResolverContext, TService> factory,
            Lifetime lifetime = Lifetime.AlwaysUnique, bool keepDefault = false) where TService : class
        {
            var serviceType = typeof(TService);
            RegisterFactory(serviceType, lifetime, keepDefault, provider =>
            {
                var context = GetContext(provider);
                var implementation = factory(context);
                return _decoratorService.Decorate(implementation, context);
            });
        }

        public void Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
        {
            RegisterFactory(serviceType, lifetime, keepDefault, provider =>
            {
                var context = GetContext(provider);
                var implementation = ActivatorUtilities.CreateInstance(provider, implementationType);
                return _decoratorService.Decorate(serviceType, implementation, context);
            });
        }

        public void RegisterType(Type serviceType, Lifetime lifetime = Lifetime.AlwaysUnique, bool keepDefault = false)
        {
            Register(serviceType, serviceType, lifetime, keepDefault);
        }

        public void RegisterGeneric(Type serviceType, Type implementationType,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
        {
            var serviceLifetime = GetLifetime(lifetime);
            var descriptor = new ServiceDescriptor(serviceType, implementationType, serviceLifetime);

            RegisterDescriptor(descriptor, keepDefault);
        }

        public void RegisterIfNotRegistered<TService, TImplementation>(Lifetime lifetime = Lifetime.AlwaysUnique)
            where TService : class where TImplementation : class, TService
        {
            Register<TService, TImplementation>(lifetime, true);
        }

        public void Decorate<TService>(Func<IResolverContext, TService, TService> factory)
        {
            _decoratorService.AddDecorator(factory);
        }

        public IRootResolver CreateResolver(bool validateRegistrations)
        {
            var serviceProvider = _collection.BuildServiceProvider(validateScopes: validateRegistrations);
            var resolver = new ServiceProviderRootResolver(serviceProvider, _collection);

            if (validateRegistrations) resolver.ValidateRegistrations();

            var bootstrapper = resolver.Resolve<IBootstrapper>();
#pragma warning disable 618
            // TODO: Figure out bootstrapping alternative for 1.0
            bootstrapper.Start();
#pragma warning restore 618

            return resolver;
        }

        private ResolverContext GetContext(IServiceProvider provider)
        {
            var resolver = new ServiceProviderResolver(provider, _collection);
            return new ResolverContext(resolver);
        }

        private void RegisterFactory(Type serviceType, Lifetime lifetime,
            bool keepDefault,
            Func<IServiceProvider, object> implementationFactory)
        {
            var serviceLifetime = GetLifetime(lifetime);
            var descriptor = new ServiceDescriptor(serviceType, implementationFactory, serviceLifetime);

            RegisterDescriptor(descriptor, keepDefault);
        }

        private void RegisterDescriptor(ServiceDescriptor descriptor, bool keepDefault = false)
        {
            if (keepDefault)
                _collection.TryAdd(descriptor);
            else
                _collection.Add(descriptor);
        }

        private ServiceLifetime GetLifetime(Lifetime lifetime)
        {
            switch (lifetime)
            {
                case Lifetime.AlwaysUnique:
                    return ServiceLifetime.Transient;
                case Lifetime.Singleton:
                    return ServiceLifetime.Singleton;
                case Lifetime.Scoped:
                    return ServiceLifetime.Scoped;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            }
        }
    }
}
