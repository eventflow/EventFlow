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
using System.Reflection;
using EventFlow.Configuration;
using EventFlow.Extensions;

namespace EventFlow.Core.IoC
{
    internal class EventFlowIoCResolver : IRootResolver
    {
        private readonly IReadOnlyDictionary<Type, List<Registration>> _registrations;
        private readonly bool _dispose;
        private readonly IResolverContext _resolverContext;
        private readonly Type[] _emptyTypeArray = {};

        public EventFlowIoCResolver(
            IReadOnlyDictionary<Type, List<Registration>> registrations,
            bool dispose)
        {
            _registrations = registrations;
            _dispose = dispose;

            _resolverContext = new ResolverContext(this);
        }

        public T Resolve<T>()
        {
            return (T) Resolve(typeof(T));
        }

        public object Resolve(Type serviceType)
        {
            List<Registration> registrations;
            var genericArguments = _emptyTypeArray;
            var typeInfo = serviceType.GetTypeInfo();

            if (serviceType.IsGenericType &&
                !_registrations.ContainsKey(serviceType) &&
                typeInfo.GetGenericTypeDefinition() != typeof(IEnumerable<>))
            {
                genericArguments = typeInfo.GetGenericArguments();
                serviceType = typeInfo.GetGenericTypeDefinition();
                typeInfo = serviceType.GetTypeInfo();
            }

            if (!_registrations.TryGetValue(serviceType, out registrations))
            {
                if (serviceType == typeof(IResolver))
                {
                    return this;
                }

                if (serviceType == typeof(IScopeResolver))
                {
                    return BeginScope();
                }

                if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    var genericMethodInfo = GetType()
                        .GetTypeInfo()
                        .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                        .Single(mi => mi.Name == nameof(ResolveEnumerable));
                    var genericArgument = typeInfo.GenericTypeArguments.Single();
                    var methodInfo = genericMethodInfo.MakeGenericMethod(genericArgument);

                    return methodInfo.Invoke(null, new object[] { _resolverContext, genericArgument });
                }

                throw new ConfigurationErrorsException($"Type {serviceType.PrettyPrint()} is not registered");
            }

            return registrations.First().Create(_resolverContext, genericArguments);
        }

        private static IEnumerable<T> ResolveEnumerable<T>(IResolverContext resolverContext, Type serviceType)
        {
            return resolverContext.Resolver
                .ResolveAll(serviceType)
                .Select(s => (T)s);
        }

        public IEnumerable<object> ResolveAll(Type serviceType)
        {
            List<Registration> registrations;

            return _registrations.TryGetValue(serviceType, out registrations)
                ? registrations.Select(r => r.Create(_resolverContext, _emptyTypeArray))
                : Enumerable.Empty<object>();
        }

        public IEnumerable<Type> GetRegisteredServices()
        {
            return _registrations.Keys
                .Where(t => !t.IsGenericTypeDefinition);
        }

        public bool HasRegistrationFor<T>()
            where T : class
        {
            return _registrations.ContainsKey(typeof(T));
        }

        public void Dispose()
        {
            if (!_dispose) return;

            foreach (var registration in _registrations.Values.SelectMany(r => r))
            {
                registration.Dispose();
            }
        }

        public IScopeResolver BeginScope()
        {
            return new EventFlowIoCResolver(_registrations, false);
        }
    }
}