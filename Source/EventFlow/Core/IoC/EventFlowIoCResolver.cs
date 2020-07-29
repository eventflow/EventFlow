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
using System.Linq;
using System.Reflection;
using EventFlow.Configuration;
using EventFlow.Extensions;

namespace EventFlow.Core.IoC
{
    internal class EventFlowIoCResolver : IRootResolver
    {
        private readonly IReadOnlyDictionary<Type, List<Registration>> _registrations;
        private readonly bool _disposeSingletons;
        private readonly IResolverContext _resolverContext;
        private readonly Type[] _emptyTypeArray = {};
        private readonly List<IDisposable> _uniqueDisposables = new List<IDisposable>();
        private readonly ConcurrentDictionary<int, object> _singletons;
        private readonly ConcurrentDictionary<int, object> _scoped = new ConcurrentDictionary<int, object>();

        public EventFlowIoCResolver(
            ConcurrentDictionary<int, object> singletons,
            IReadOnlyDictionary<Type, List<Registration>> registrations,
            bool disposeSingletons)
        {
            _singletons = singletons;
            _registrations = registrations;
            _disposeSingletons = disposeSingletons;

            _resolverContext = new ResolverContext(this);
        }

        public T Resolve<T>()
        {
            return (T) Resolve(typeof(T));
        }

        public object Resolve(Type serviceType)
        {
            var genericArguments = _emptyTypeArray;
            var typeInfo = serviceType.GetTypeInfo();

            if (typeInfo.IsGenericType &&
                !_registrations.ContainsKey(serviceType) &&
                typeInfo.GetGenericTypeDefinition() != typeof(IEnumerable<>))
            {
                genericArguments = typeInfo.GetGenericArguments();
                serviceType = typeInfo.GetGenericTypeDefinition();
                typeInfo = serviceType.GetTypeInfo();
            }

            IRegistration registration;

            if (!_registrations.TryGetValue(serviceType, out var registrations))
            {
                if (serviceType == typeof(IResolver))
                {
                    return this;
                }

                if (serviceType == typeof(IScopeResolver))
                {
                    return new EventFlowIoCResolver(_singletons, _registrations, false);
                }

                if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    registration = EnumerableResolvers.GetOrAdd(serviceType, t => new EnumerableResolver(t));
                    return Create(registration);
                }

                throw new Exception($"Type {serviceType.PrettyPrint()} is not registered");
            }

            registration = registrations.First();
            return Create(registration, genericArguments);
        }

        public IEnumerable<object> ResolveAll(Type serviceType)
        {
            return _registrations.TryGetValue(serviceType, out var registrations)
                ? registrations.Select(r => Create(r, _emptyTypeArray))
                : Enumerable.Empty<object>();
        }

        public IEnumerable<Type> GetRegisteredServices()
        {
            return _registrations.Keys
                .Where(t => !t.GetTypeInfo().IsGenericTypeDefinition);
        }

        public bool HasRegistrationFor<T>()
            where T : class
        {
            return _registrations.ContainsKey(typeof(T));
        }

        public void Dispose()
        {
            foreach (var disposable in _uniqueDisposables)
            {
                disposable.Dispose();
            }
            _uniqueDisposables.Clear();

            foreach (var scoped in _scoped.Values)
            {
                (scoped as IDisposable)?.Dispose();
            }
            _scoped.Clear();

            if (!_disposeSingletons)
            {
                return;
            }

            foreach (var singleton in _singletons.Values)
            {
                (singleton as IDisposable)?.Dispose();
            }
            _singletons.Clear();
        }

        public IScopeResolver BeginScope()
        {
            return new EventFlowIoCResolver(_singletons, _registrations, false);
        }

        private object Create(IRegistration registration, params Type[] genericTypeArguments)
        {
            switch (registration.Lifetime)
            {
                case Lifetime.AlwaysUnique:
                    var service = registration.Create(_resolverContext, genericTypeArguments);
                    if (service is IDisposable disposable)
                    {
                        _uniqueDisposables.Add(disposable);
                    }
                    return service;

                case Lifetime.Singleton:
                    return _singletons.GetOrAdd(
                        CombineHash(registration.ServiceType, genericTypeArguments),
                        _ => registration.Create(_resolverContext, genericTypeArguments));

                case Lifetime.Scoped:
                    return _scoped.GetOrAdd(
                        CombineHash(registration.ServiceType, genericTypeArguments),
                        _ => registration.Create(_resolverContext, genericTypeArguments));

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static int CombineHash(Type serviceType, params Type[] genericTypeArguments)
        {
            return genericTypeArguments.Aggregate(
                serviceType.GetHashCode(),
                (i, t) => HashHelper.Combine(i, t.GetHashCode()));
        }

        private static readonly ConcurrentDictionary<Type, IRegistration> EnumerableResolvers = new ConcurrentDictionary<Type, IRegistration>();

        private class EnumerableResolver : IRegistration
        {
            public Type ServiceType { get; }
            public Lifetime Lifetime => Lifetime.AlwaysUnique;

            private readonly MethodInfo _methodInfo;

            public EnumerableResolver(
                Type typeInfo)
            {
                var genericMethodInfo = GetType()
                    .GetTypeInfo()
                    .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                    .Single(mi => mi.Name == nameof(ResolveEnumerable));

                ServiceType = typeInfo.GenericTypeArguments.Single();
                _methodInfo = genericMethodInfo.MakeGenericMethod(ServiceType);
            }

            private static IEnumerable<T> ResolveEnumerable<T>(IResolverContext resolverContext, Type serviceType)
            {
                return resolverContext.Resolver
                    .ResolveAll(serviceType)
                    .Select(s => (T)s);
            }

            public object Create(IResolverContext resolverContext, Type[] genericTypeArguments)
            {
                return _methodInfo.Invoke(null, new object[] { resolverContext, ServiceType });
            }
        }
    }
}
