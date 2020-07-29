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

namespace EventFlow.Configuration.Decorators
{
    public class DecoratorService : IDecoratorService
    {
        private readonly ConcurrentDictionary<Type, List<Func<object, IResolverContext, object>>> _decorators = new ConcurrentDictionary<Type, List<Func<object, IResolverContext, object>>>();
        private readonly ConcurrentDictionary<Type, Func<object, IResolverContext, object>> _genericDecoratorCall = new ConcurrentDictionary<Type, Func<object, IResolverContext, object>>(); 

        public object Decorate(Type serviceType, object implementation, IResolverContext resolverContext)
        {
            var decorate = _genericDecoratorCall.GetOrAdd(
                serviceType,
                t =>
                {
                    var genericMethodInfo = GetType()
                        .GetTypeInfo()
                        .GetMethods()
                        .Single(mi => mi.IsGenericMethodDefinition && mi.Name == "Decorate");
                    var methodInfo = genericMethodInfo.MakeGenericMethod(t);
                    return (o, r) => methodInfo.Invoke(this, new[] {o, r});
                });
            return decorate(implementation, resolverContext);
        }

        public TService Decorate<TService>(TService implementation, IResolverContext resolverContext)
        {
            List<Func<object, IResolverContext, object>> decorators;
            return !_decorators.TryGetValue(typeof(TService), out decorators)
                ? implementation
                : decorators.Aggregate(implementation, (current, decorator) => (TService) decorator(current, resolverContext));
        }

        public void AddDecorator<TService>(Func<IResolverContext, TService, TService> factory)
        {
            var decorators = _decorators.GetOrAdd(
                typeof(TService),
                new List<Func<object, IResolverContext, object>>());
            decorators.Add((s, c) => factory(c, (TService) s));
        }
    }
}