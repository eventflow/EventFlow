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
using System.Collections.Generic;
using System.Linq;
using EventFlow.Logs.Internals.Logging;

namespace EventFlow.Logs
{
    public class LibLog : Log
    {
        private readonly Logger _logger;

        protected override bool IsVerboseEnabled { get; } = true;
        protected override bool IsInformationEnabled { get; } = true;
        protected override bool IsDebugEnabled { get; } = true;

        private static readonly IReadOnlyDictionary<LogLevel, Internals.Logging.LogLevel> LevelMap = new Dictionary<LogLevel, Internals.Logging.LogLevel>
            {
                {LogLevel.Verbose, Internals.Logging.LogLevel.Trace},
                {LogLevel.Debug, Internals.Logging.LogLevel.Debug},
                {LogLevel.Information, Internals.Logging.LogLevel.Info},
                {LogLevel.Warning, Internals.Logging.LogLevel.Warn},
                {LogLevel.Error, Internals.Logging.LogLevel.Error},
                {LogLevel.Fatal, Internals.Logging.LogLevel.Fatal}
            };

        public LibLog(ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger("EventFlow");
        }

        public override void Write(LogLevel logLevel, string format, params object[] args)
        {
            _logger(
                LevelMap[logLevel],
                () => args.Any() ? string.Format(format, args) : format);
        }

        public override void Write(LogLevel logLevel, Exception exception, string format, params object[] args)
        {
            _logger(
                LevelMap[logLevel],
                () => args.Any() ? string.Format(format, args) : format,
                exception);
        }

        public override void Verbose(Func<string> combersomeLogging)
        {
            _logger(
                Internals.Logging.LogLevel.Trace,
                combersomeLogging);
        }

        public override void Debug(Func<string> combersomeLogging)
        {
            _logger(
                Internals.Logging.LogLevel.Debug,
                combersomeLogging);
        }
    }
}
