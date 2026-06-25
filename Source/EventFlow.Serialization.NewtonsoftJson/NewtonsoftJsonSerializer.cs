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
using EventFlow.Core;
using Newtonsoft.Json;

namespace EventFlow.Serialization.NewtonsoftJson
{
    public class NewtonsoftJsonSerializer : IJsonSerializer
    {
        private readonly JsonSerializerSettings _settingsIndented;
        private readonly JsonSerializerSettings _settingsNotIndented;

        public NewtonsoftJsonSerializer(Action<JsonSerializerSettings> configure = null)
        {
            _settingsIndented = CreateSettings(configure, Formatting.Indented);
            _settingsNotIndented = CreateSettings(configure, Formatting.None);
        }

        private static JsonSerializerSettings CreateSettings(Action<JsonSerializerSettings> configure, Formatting formatting)
        {
            var settings = new JsonSerializerSettings();
            configure?.Invoke(settings);
            settings.Formatting = formatting;
            return settings;
        }

        public string Serialize(object obj, bool indented = false)
        {
            var settings = indented ? _settingsIndented : _settingsNotIndented;
            return JsonConvert.SerializeObject(obj, settings);
        }

        public object Deserialize(string json, Type type)
        {
            return JsonConvert.DeserializeObject(json, type, _settingsNotIndented);
        }

        public T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, _settingsNotIndented);
        }
    }
}
