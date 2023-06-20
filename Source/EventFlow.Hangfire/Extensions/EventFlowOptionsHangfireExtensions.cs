// The MIT License (MIT)
// 
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
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
using EventFlow.Hangfire.Integration;
using EventFlow.Jobs;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace EventFlow.Hangfire.Extensions
{
    public static class EventFlowOptionsHangfireExtensions
    {
        public static IEventFlowOptions UseHangfireJobScheduler(
            this IEventFlowOptions eventFlowOptions)
        {
            eventFlowOptions.ServiceCollection.AddTransient<IJobScheduler, HangfireJobScheduler>();
            eventFlowOptions.ServiceCollection.AddTransient<IHangfireJobRunner, HangfireJobRunner>();
            eventFlowOptions.ServiceCollection.AddTransient<IJobDisplayNameBuilder, JobDisplayNameBuilder>();
            eventFlowOptions.ServiceCollection.AddTransient<IBackgroundJobClient, BackgroundJobClient>();
            eventFlowOptions.ServiceCollection.AddTransient<IQueueNameProvider>(r => new QueueNameProvider(null));

            return eventFlowOptions;
        }

        public static IEventFlowOptions UseHangfireJobScheduler(
            this IEventFlowOptions eventFlowOptions,
            Action<IEventFlowHangfireOptions> configurationAction)
        {
            var options = eventFlowOptions.UseHangfireJobScheduler();
            configurationAction(new EventFlowHangfireOptions(options));
            return options;
        }
    }
}