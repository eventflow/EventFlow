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
using JetBrains.Annotations;

namespace EventFlow.Logs
{
    public interface ILog
    {
        [StringFormatMethod("format")]
        void Verbose(string format, params object[] args);

        [StringFormatMethod("format")]
        void Verbose(Exception exception, string format, params object[] args);
        
        void Verbose(Func<string> combersomeLogging);
        
        void Verbose(Action<StringBuilder> combersomeLogging);

        [StringFormatMethod("format")]
        void Debug(string format, params object[] args);

        [StringFormatMethod("format")]
        void Debug(Exception exception, string format, params object[] args);
        
        void Debug(Func<string> combersomeLogging);
        
        void Debug(Action<StringBuilder> combersomeLogging);

        [StringFormatMethod("format")]
        void Information(string format, params object[] args);

        [StringFormatMethod("format")]
        void Information(Exception exception, string format, params object[] args);

        void Information(Func<string> combersomeLogging);
        void Information(Action<StringBuilder> combersomeLogging);

        [StringFormatMethod("format")]
        void Warning(string format, params object[] args);

        [StringFormatMethod("format")]
        void Warning(Exception exception, string format, params object[] args);

        [StringFormatMethod("format")]
        void Error(string format, params object[] args);

        [StringFormatMethod("format")]
        void Error(Exception exception, string format, params object[] args);

        [StringFormatMethod("format")]
        void Fatal(string format, params object[] args);

        [StringFormatMethod("format")]
        void Fatal(Exception exception, string format, params object[] args);

        [StringFormatMethod("format")]
        void Write(LogLevel logLevel, string format, params object[] args);

        [StringFormatMethod("format")]
        void Write(LogLevel logLevel, Exception exception, string format, params object[] args);
    }
}