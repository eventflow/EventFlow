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
using System.Text.Json;
using EventFlow.Core;

namespace EventFlow.Serialization.SystemTextJson
{
    public class SystemTextJsonSerializer : IJsonSerializer
    {
        private readonly JsonSerializerOptions _optionsIndented;
        private readonly JsonSerializerOptions _optionsNotIndented;

        public SystemTextJsonSerializer(Action<JsonSerializerOptions> configure = null)
        {
            _optionsNotIndented = CreateOptions(configure, false);
            _optionsIndented = CreateOptions(configure, true);
        }

        private static JsonSerializerOptions CreateOptions(Action<JsonSerializerOptions> configure, bool indented)
        {
            var options = new JsonSerializerOptions();
            configure?.Invoke(options);
            options.WriteIndented = indented;
            return options;
        }

        public string Serialize(object obj, bool indented = false)
        {
            var options = indented ? _optionsIndented : _optionsNotIndented;
            return System.Text.Json.JsonSerializer.Serialize(obj, obj?.GetType() ?? typeof(object), options);
        }

        public object Deserialize(string json, Type type)
        {
            return System.Text.Json.JsonSerializer.Deserialize(json, type, _optionsNotIndented);
        }

        public T Deserialize<T>(string json)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, _optionsNotIndented);
        }
    }
}
