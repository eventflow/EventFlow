// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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
using EventFlow.Core;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace EventFlow.TestHelpers
{
    public class LoggerMock<T> : ILogger<T>
    {
        private static IReadOnlyCollection<LogMessage> Empty = new LogMessage[] { };

        public class LogMessage
        {
            public string Message { get; }
            public Exception Exception { get; }

            public LogMessage(
                string message,
                Exception exception)
            {
                Message = message;
                Exception = exception;
            }
        }

        private readonly ConcurrentDictionary<LogLevel, List<LogMessage>> _logMessages = new ConcurrentDictionary<LogLevel, List<LogMessage>>();

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);
            var list = _logMessages.GetOrAdd(
                logLevel,
                _ => new List<LogMessage>());
            list.Add(new LogMessage(message, exception));
        }

        public bool IsEnabled(LogLevel _)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return DisposableAction.Empty;
        }

        public void VerifyNoProblems()
        {
            var messages = Logs(LogLevel.Critical, LogLevel.Error)
                .Select(m => m.Message)
                .ToList();
            messages.Should().BeEmpty(string.Join(", ", messages));
        }

        public void VerifyProblemLogged(params Exception[] expectedExceptions)
        {
            var exceptions = Logs(LogLevel.Error, LogLevel.Critical)
                .Select(m => m.Exception)
                .ToList();
            exceptions.Should().AllBeEquivalentTo(expectedExceptions);
        }

        public IReadOnlyCollection<LogMessage> Logs(params LogLevel[] logLevels)
        {
            return logLevels
                .SelectMany(l => _logMessages.TryGetValue(l, out var list) ? list : Empty)
                .ToList();
        }
    }
}
