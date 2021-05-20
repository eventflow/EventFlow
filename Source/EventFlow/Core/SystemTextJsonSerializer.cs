using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace EventFlow.Core
{
    public class SystemTextJsonSerializer : IJsonSerializer
    {
        private readonly JsonSerializerOptions _optionsIndented = new JsonSerializerOptions();
        private readonly JsonSerializerOptions _optionsNotIndented = new JsonSerializerOptions();

        public SystemTextJsonSerializer(Action<JsonSerializerOptions> options = default)
        {
            options?.Invoke(_optionsIndented);
            options?.Invoke(_optionsNotIndented);

            _optionsIndented.WriteIndented = true;
            _optionsNotIndented.WriteIndented = false;
        }

        public string Serialize(object obj, bool indented = false)
        {
            var settings = indented ? _optionsIndented : _optionsNotIndented;
            return System.Text.Json.JsonSerializer.Serialize(obj, settings);
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
