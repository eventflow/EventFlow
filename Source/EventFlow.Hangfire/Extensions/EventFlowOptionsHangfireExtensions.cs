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
using EventFlow.Hangfire.Integration;
using EventFlow.Jobs;
using Hangfire;

namespace EventFlow.Hangfire.Extensions
{
    public static class EventFlowOptionsHangfireExtensions
    {
        [Obsolete("Please use the correctly spelled 'UseHangfireJobScheduler()' instead")]
        public static IEventFlowOptions UseHandfireJobScheduler(
            this IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions.UseHangfireJobScheduler();
        }

        public static IEventFlowOptions UseHangfireJobScheduler(
            this IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions.RegisterServices(sr =>
                {
                    sr.Register<IJobScheduler, HangfireJobScheduler>();
                    sr.Register<IHangfireJobRunner, HangfireJobRunner>();
                    sr.Register<IJobDisplayNameBuilder, JobDisplayNameBuilder>();
                    sr.Register<IBackgroundJobClient>(r => new BackgroundJobClient());
                });
        }
    }
}