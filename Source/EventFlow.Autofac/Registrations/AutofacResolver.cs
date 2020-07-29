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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Core;
using EventFlow.Configuration;

namespace EventFlow.Autofac.Registrations
{
    internal class AutofacResolver : IResolver
    {
        private readonly IComponentContext _componentContext;

        public AutofacResolver(IComponentContext componentContext)
        {
            _componentContext = componentContext;
        }

        public T Resolve<T>()
        {
            return _componentContext.Resolve<T>();
        }

        public T Resolve<T>(params Parameter[] parameters)
        {
            return _componentContext.Resolve<T>(parameters);
        }

        public object Resolve(Type serviceType)
        {
            return _componentContext.Resolve(serviceType);
        }

        public object Resolve(Type serviceType, params Parameter[] parameters)
        {
            return _componentContext.Resolve(serviceType, parameters);
        }

        public IEnumerable<object> ResolveAll(Type serviceType)
        {
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(serviceType);
            return ((IEnumerable) _componentContext.Resolve(enumerableType)).OfType<object>().ToList();
        }

        public IEnumerable<Type> GetRegisteredServices()
        {
            return _componentContext.ComponentRegistry.Registrations
                .SelectMany(x => x.Services)
                .OfType<TypedService>()
                .Where(x => !x.ServiceType.Name.StartsWith("Autofac"))
                .Select(x => x.ServiceType);
        }

        public bool HasRegistrationFor<T>()
            where T : class
        {
            var serviceType = typeof(T);
            return GetRegisteredServices().Any(t => serviceType == t);
        }
    }
}