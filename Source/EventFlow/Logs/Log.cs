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
using System.Text;

namespace EventFlow.Logs
{
    public abstract class Log : ILog
    {
        protected abstract bool IsVerboseEnabled { get; }
        protected abstract bool IsInformationEnabled { get; }
        protected abstract bool IsDebugEnabled { get; }

        public abstract void Write(LogLevel logLevel, string format, params object[] args);

        public abstract void Write(LogLevel logLevel, Exception exception, string format, params object[] args);

        public void Verbose(string format, params object[] args)
        {
            Write(LogLevel.Verbose, format, args);
        }

        public void Verbose(Exception exception, string format, params object[] args)
        {
            Write(LogLevel.Verbose, exception, format, args);
        }

        public virtual void Verbose(Func<string> combersomeLogging)
        {
            if (!IsVerboseEnabled)
            {
                return;
            }

            Verbose(combersomeLogging());
        }

        public void Verbose(Action<StringBuilder> combersomeLogging)
        {
            Verbose(() =>
                {
                    var stringBuilder = new StringBuilder();
                    combersomeLogging(stringBuilder);
                    return stringBuilder.ToString();
                });
        }

        public void Debug(string format, params object[] args)
        {
            Write(LogLevel.Debug, format, args);
        }

        public void Debug(Exception exception, string format, params object[] args)
        {
            Write(LogLevel.Debug, exception, format, args);
        }

        public virtual void Debug(Func<string> combersomeLogging)
        {
            if (!IsDebugEnabled)
            {
                return;
            }
            
            Debug(combersomeLogging());
        }

        public void Debug(Action<StringBuilder> combersomeLogging)
        {
            Debug(() =>
                {
                    var stringBuilder = new StringBuilder();
                    combersomeLogging(stringBuilder);
                    return stringBuilder.ToString();
                });
        }

        public void Information(string format, params object[] args)
        {
            Write(LogLevel.Information, format, args);
        }

        public void Information(Exception exception, string format, params object[] args)
        {
            Write(LogLevel.Information, exception, format, args);
        }

        public virtual void Information(Func<string> combersomeLogging)
        {
            if (!IsInformationEnabled)
            {
                return;
            }

            Information(combersomeLogging());
        }

        public void Information(Action<StringBuilder> combersomeLogging)
        {
            Information(() =>
                {
                    var stringBuilder = new StringBuilder();
                    combersomeLogging(stringBuilder);
                    return stringBuilder.ToString();
                });
        }

        public void Warning(string format, params object[] args)
        {
            Write(LogLevel.Warning, format, args);
        }

        public void Warning(Exception exception, string format, params object[] args)
        {
            Write(LogLevel.Warning, exception, format, args);
        }

        public void Error(string format, params object[] args)
        {
            Write(LogLevel.Error, format, args);
        }

        public void Error(Exception exception, string format, params object[] args)
        {
            Write(LogLevel.Error, exception, format, args);
        }

        public void Fatal(string format, params object[] args)
        {
            Write(LogLevel.Fatal, format, args);
        }

        public void Fatal(Exception exception, string format, params object[] args)
        {
            Write(LogLevel.Fatal, exception, format, args);
        }
    }
}