// The MIT License (MIT)
//
// Copyright (c) 2015-2025 Rasmus Mikkelsen
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

namespace EventFlow.Core
{
    /// <summary>
    /// Default <see cref="IJsonSerializer"/> registered by EventFlow when no JSON
    /// serializer has been configured. It throws on first use to fail fast with an
    /// actionable message instead of producing an opaque dependency resolution error.
    /// Install one of the serializer packages and register it during configuration:
    /// <c>AddNewtonsoftJson()</c> (EventFlow.Serialization.NewtonsoftJson) or
    /// <c>AddSystemTextJson()</c> (EventFlow.Serialization.SystemTextJson).
    /// </summary>
    public class ThrowingJsonSerializer : IJsonSerializer
    {
        private const string Message =
            "No JSON serializer has been registered. Install one of the EventFlow serializer " +
            "packages and register it during configuration, e.g. '.AddNewtonsoftJson()' " +
            "(EventFlow.Serialization.NewtonsoftJson) or '.AddSystemTextJson()' " +
            "(EventFlow.Serialization.SystemTextJson).";

        public string Serialize(object obj, bool indented = false) => throw new InvalidOperationException(Message);

        public object Deserialize(string json, Type type) => throw new InvalidOperationException(Message);

        public T Deserialize<T>(string json) => throw new InvalidOperationException(Message);
    }
}
