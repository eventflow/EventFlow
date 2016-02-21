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
using System.Linq.Expressions;
using System.Threading;
using DryIoc;
using EventFlow.Extensions;

namespace EventFlow.Configuration.Registrations
{
    public class DryIocServiceRegistration : IServiceRegistration
    {
        private readonly IContainer _container = new Container();

        private class DryIocResolverContext : IResolverContext
        {
            public IResolver Resolver { get; }
        }

        public void Register<TService, TImplementation>(
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
            where TService : class
            where TImplementation : class, TService
        {
            _container.Register<TService, TImplementation>(setup: Setup.With(allowDisposableTransient: true));
        }

        public void Register<TService>(
            Func<IResolverContext, TService> factory,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
            where TService : class
        {
            _container.Register(Made.Of(() => factory(Arg.Of<IResolverContext>())), setup: Setup.With(allowDisposableTransient: true));
        }

        public void Register(
            Type serviceType,
            Type implementationType,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
        {
            _container.Register(serviceType, implementationType, setup: Setup.With(allowDisposableTransient: true));
        }

        public void RegisterType(
            Type serviceType,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
        {
            _container.Register(serviceType, setup: Setup.With(allowDisposableTransient: true));
        }

        public void RegisterGeneric(
            Type serviceType,
            Type implementationType,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
        {
            _container.Register(serviceType, implementationType, setup: Setup.With(allowDisposableTransient: true));
        }

        public void RegisterIfNotRegistered<TService, TImplementation>(
            Lifetime lifetime = Lifetime.AlwaysUnique)
            where TService : class
            where TImplementation : class, TService
        {
            throw new NotImplementedException();
        }

        public void Decorate<TService>(Func<IResolverContext, TService, TService> factory)
        {
            throw new NotImplementedException();
        }

        public IRootResolver CreateResolver(bool validateRegistrations)
        {
            _container.Resolve<IEnumerable<IBootstrap>>().Boot(CancellationToken.None);

            return new DryIocResolver(_container);
        }
    }
}