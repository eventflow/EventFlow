// The MIT License (MIT)
//
// Copyright (c) 2015 EventFlow
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

namespace EventFlow.Configuration
{
    public enum Lifetime
    {
        AlwaysUnique,
        Singleton,
    }

    public abstract class DiRegistration
    {
        public Type ServiceType { get; protected set; }
        public Lifetime Lifetime { get; protected set; }

        internal abstract void Configure(ContainerBuilder containerBuilder);
    }

    public class DiRegistration<TService> : DiRegistration
        where TService : class
    {
        public Func<IResolver, object> Factory { get; protected set; }

        public DiRegistration(Func<IResolver, TService> factory, Lifetime lifetime = Lifetime.AlwaysUnique)
        {
            ServiceType = typeof (TService);
            Factory = factory;
            Lifetime = lifetime;
        }

        internal override void Configure(ContainerBuilder containerBuilder)
        {
            switch (Lifetime)
            {
                case Lifetime.AlwaysUnique:
                    containerBuilder.Register(cc => Factory(new AutofacResolver(cc))).As(ServiceType);
                    break;
                case Lifetime.Singleton:
                    containerBuilder.Register(cc => Factory(new AutofacResolver(cc))).As(ServiceType).SingleInstance();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public class DiRegistration<TService, TImplementation> : DiRegistration
        where TImplementation : class, TService
    {
        public DiRegistration(Lifetime lifetime = Lifetime.AlwaysUnique)
        {
            Lifetime = lifetime;
            ServiceType = typeof (TService);
        }

        internal override void Configure(ContainerBuilder containerBuilder)
        {
            switch (Lifetime)
            {
                case Lifetime.AlwaysUnique:
                    containerBuilder.RegisterType<TImplementation>().As<TService>();
                    break;
                case Lifetime.Singleton:
                    containerBuilder.RegisterType<TImplementation>().As<TService>().SingleInstance();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
