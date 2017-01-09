// The MIT License (MIT)
// 
// Copyright (c) 2015-2017 Rasmus Mikkelsen
// Copyright (c) 2015-2017 eBay Software Foundation
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
using Autofac;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Configuration.Registrations;
using EventFlow.Configuration.Registrations.Services;

namespace EventFlow.Autofac.Extensions
{
    public static class EventFlowOptionsAutofacExtensions
    {
        public static IEventFlowOptions UseAutofacContainerBuilder(
            this IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions
                .UseAutofacContainerBuilder(new ContainerBuilder());
        }

        public static IEventFlowOptions UseAutofacContainerBuilder(
            this IEventFlowOptions eventFlowOptions,
            ContainerBuilder containerBuilder)
        {
            return eventFlowOptions
                .UseServiceRegistration(new AutofacServiceRegistration(containerBuilder));
        }

        public static IEventFlowOptions UseAutofacAggregateRootFactory(
            this IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions
                .RegisterServices(f => f.Register<IAggregateFactory, AutofacAggregateRootFactory>(Lifetime.Singleton));
        }

        public static IContainer CreateContainer(
            this IEventFlowOptions eventFlowOptions,
            bool validateRegistrations = true)
        {
            var rootResolver = eventFlowOptions.CreateResolver(validateRegistrations);
            var autofacRootResolver = rootResolver as AutofacRootResolver;
            if (autofacRootResolver == null)
            {
                throw new InvalidOperationException(
                    "Make sure to configure the EventFlowOptions for Autofac using the .UseAutofacContainerBuilder(...)");
            }

            return autofacRootResolver.Container;
        }
    }
}
