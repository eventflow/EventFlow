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
using System.Configuration;
using System.Linq;
using EventFlow.Configuration;
using EventFlow.Extensions;

namespace EventFlow.Core.FlowIoC
{
    internal class FlowIoCResolver : IRootResolver
    {
        private readonly IReadOnlyDictionary<Type, List<Registration>> _registrations;
        private readonly IResolverContext _resolverContext;

        public FlowIoCResolver(
            IReadOnlyDictionary<Type, List<Registration>> registrations)
        {
            _registrations = registrations;

            _resolverContext = new ResolverContext(this);
        }

        public T Resolve<T>()
        {
            return (T) Resolve(typeof(T));
        }

        public object Resolve(Type serviceType)
        {
            List<Registration> registrations;
            if (!_registrations.TryGetValue(serviceType, out registrations))
            {
                throw new ConfigurationErrorsException($"Type {serviceType.PrettyPrint()} is not registered");
            }

            return registrations.First().Create(_resolverContext);
        }

        public IEnumerable<object> ResolveAll(Type serviceType)
        {
            List<Registration> registrations;
            if (!_registrations.TryGetValue(serviceType, out registrations))
            {
                throw new ConfigurationErrorsException($"Type {serviceType.PrettyPrint()} is not registered");
            }

            return registrations
                .Select(r => r.Create(_resolverContext));
        }

        public IEnumerable<Type> GetRegisteredServices()
        {
            return _registrations.Keys;
        }

        public bool HasRegistrationFor<T>()
            where T : class
        {
            return _registrations.ContainsKey(typeof(T));
        }

        public void Dispose()
        {
        }

        public IScopeResolver BeginScope()
        {
            throw new NotImplementedException();
        }
    }
}