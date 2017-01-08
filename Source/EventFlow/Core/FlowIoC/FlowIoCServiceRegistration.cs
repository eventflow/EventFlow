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

using System;
using System.Collections.Generic;
using EventFlow.Configuration;
using EventFlow.Core.FlowIoC.Factories;

namespace EventFlow.Core.FlowIoC
{
    public class FlowIoCServiceRegistration : IServiceRegistration
    {
        private readonly object _syncRoot = new object();
        private readonly Dictionary<Type, List<Registration>> _registrations = new Dictionary<Type, List<Registration>>();

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
        }

        public void RegisterIfNotRegistered<TService, TImplementation>(
            Lifetime lifetime = Lifetime.AlwaysUnique)
            where TService : class
            where TImplementation : class, TService
        {
        }

        public void Decorate<TService>(
            Func<IResolverContext, TService, TService> factory)
        {
        }

        public IRootResolver CreateResolver(bool validateRegistrations)
        {
            return new FlowIoCResolver(_registrations);
        }

        private void Register(
            Type serviceType,
            IFactory factory,
            Lifetime lifetime,
            bool keepDefault)
        {
            lock (_syncRoot)
            {
                List<Registration> registrations;
                if (_registrations.TryGetValue(serviceType, out registrations))
                {
                    if (!keepDefault)
                    {
                        registrations.Clear();
                    }
                }
                else
                {
                    registrations = new List<Registration>();
                    _registrations.Add(serviceType, registrations);
                }

                registrations.Insert(0, new Registration(lifetime, factory));
            }
        }
    }
}