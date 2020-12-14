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
using EventFlow.Configuration;
using EventFlow.Logs;
using EventFlow.Logs.Internals.Logging;
using EventFlow.Logs.Internals.Logging.LogProviders;
using ILog = EventFlow.Logs.ILog;

namespace EventFlow.Extensions
{
    public enum LibLogProviders
    {
        EntLib,
        Log4Net,
        Loupe,
        NLog,
        Serilog
    }

    public static class EventFlowOptionsLogExtensions
    {
        public static IEventFlowOptions UseNullLog(
            this IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions.RegisterServices(sr => sr.Register<ILog, NullLog>());
        }

        public static IEventFlowOptions UseConsoleLog(
            this IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions.RegisterServices(sr => sr.Register<ILog, ConsoleLog>());
        }

        public static IEventFlowOptions UseLibLog(
            this IEventFlowOptions eventFlowOptions,
            LibLogProviders libLogProviders)
        {
            ILogProvider logProvider;
            switch (libLogProviders)
            {
                case LibLogProviders.EntLib:
                    logProvider = new EntLibLogProvider();
                    break;
                case LibLogProviders.Log4Net:
                    logProvider = new Log4NetLogProvider();
                    break;
                case LibLogProviders.Loupe:
                    logProvider = new LoupeLogProvider();
                    break;
                case LibLogProviders.NLog:
                    logProvider = new NLogLogProvider();
                    break;
                case LibLogProviders.Serilog:
                    logProvider = new SerilogLogProvider();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(libLogProviders), libLogProviders, null);
            }

            var log = new LibLog(logProvider);
            return eventFlowOptions
                .RegisterServices(sr => sr.Register<ILog>(_ => log, Lifetime.Singleton));
        }
    }
}
