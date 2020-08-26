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
using EventFlow.Logs;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace EventFlow.AspNetCore.Logging
{
    public class AspNetCoreLoggerLog : Log
    {
        private readonly ILogger _logger;

        public AspNetCoreLoggerLog(ILogger<EventFlowAspNetCore> logger)
        {
            _logger = logger;
        }

        protected override bool IsVerboseEnabled => _logger.IsEnabled(LogLevel.Trace);

        protected override bool IsInformationEnabled => _logger.IsEnabled(LogLevel.Information);

        protected override bool IsDebugEnabled => _logger.IsEnabled(LogLevel.Debug);

        public override void Write(Logs.LogLevel logLevel, string format, params object[] args)
        {
            _logger.Log((LogLevel)logLevel, format, args);
        }

        public override void Write(Logs.LogLevel logLevel, Exception exception, string format, params object[] args)
        {
            _logger.Log((LogLevel)logLevel, exception, format, args);
        }
    }
}
