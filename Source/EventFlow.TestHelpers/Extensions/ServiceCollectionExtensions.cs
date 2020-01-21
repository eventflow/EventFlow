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
using System.Linq;
using EventFlow.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventFlow.TestHelpers.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void Decorate<T>(
            this IServiceCollection services,
            Func<IServiceProvider, T, T> factory)
            where T : class
        {
            var originalDescriptor = services.Single(s => s.ServiceType == typeof(T));
            if (originalDescriptor == null)
            {
                throw new InvalidOperationException($"No service of type '{typeof(T).PrettyPrint()}' registered");
            }

            services.Replace(ServiceDescriptor.Describe(
                typeof(T),
                p => factory(p, (T) CreateInstance(p, originalDescriptor)),
                originalDescriptor.Lifetime));
        }

        private static object CreateInstance(
            IServiceProvider services,
            ServiceDescriptor descriptor)
        {
            if (descriptor.ImplementationInstance != null)
            {
                return descriptor.ImplementationInstance;
            }

            if (descriptor.ImplementationFactory != null)
            {
                return descriptor.ImplementationFactory(services);
            }

            return ActivatorUtilities.GetServiceOrCreateInstance(
                services, 
                descriptor.ImplementationType);
        }
    }
}
