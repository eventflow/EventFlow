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

using EventFlow.Commands;
using EventFlow.ValueObjects;
using System;
using System.Text.Json;

namespace EventFlow.Core
{
    public class DefaultJsonSerializer : IJsonSerializer
    {
        private readonly JsonSerializerOptions _settingsNotIndented = new JsonSerializerOptions();
        private readonly JsonSerializerOptions _settingsIndented = new JsonSerializerOptions();
        
        public DefaultJsonSerializer()
            : this(new JsonSerializerOptions())
        { }

        public DefaultJsonSerializer(JsonSerializerOptions options)
        {
            var baseSettings = new JsonSerializerOptions();
            baseSettings.Converters.Add(new SourceIdInterfaceConverter());

            var indentedSettings = new JsonSerializerOptions(baseSettings);
            indentedSettings.WriteIndented = true;

            options.PropertyNameCaseInsensitive = true;
            _settingsIndented = indentedSettings;

            var nonIndentedSettings = new JsonSerializerOptions(baseSettings);
            nonIndentedSettings.WriteIndented = false;
            _settingsNotIndented = nonIndentedSettings;
        }

        public string Serialize(object obj, bool indented = false)
        {
            var settings = indented ? _settingsIndented : _settingsNotIndented;
            return JsonSerializer.Serialize(obj, settings);
        }

        public object Deserialize(string json, Type type)
        {
            return JsonSerializer.Deserialize(json, type, _settingsNotIndented);
        }

        public T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _settingsNotIndented);
        }
    }
}