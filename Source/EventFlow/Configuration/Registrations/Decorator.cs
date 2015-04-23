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
using Autofac;
using EventFlow.Configuration.Registrations.Resolvers;

namespace EventFlow.Configuration.Registrations
{
    internal abstract class Decorator
    {
        public abstract Type ServiceType { get; }
        public abstract void Configure(ContainerBuilder containerBuilder, int level);

        protected string GetKey(int level)
        {
            if (level <= 0)
            {
                throw new ArgumentOutOfRangeException("level");
            }

            return string.Format("{0} (level {1})", ServiceType.FullName, level);
        }
    }

    internal class Decorator<TService> : Decorator
    {
        private readonly Func<IResolverContext, TService, TService> _factory;
        private readonly Type _serviceType = typeof(TService);
        public override Type ServiceType { get { return _serviceType; } }

        public Decorator(Func<IResolverContext, TService, TService> factory)
        {
            _factory = factory;
        }

        public override void Configure(ContainerBuilder containerBuilder, int level)
        {
            containerBuilder.RegisterDecorator<TService>(
                (r, inner) => _factory(new AutofacResolverContext(new AutofacResolver(r)), inner),
                GetKey(level));
        }
    }
}
