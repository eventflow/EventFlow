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
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using EventFlow.ValueObjects;

namespace EventFlow.Serialization.SystemTextJson
{
    public class SingleValueObjectConverterFactory : JsonConverterFactory
    {
        private static readonly ConcurrentDictionary<Type, JsonConverter> Converters =
            new ConcurrentDictionary<Type, JsonConverter>();

        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(ISingleValueObject).IsAssignableFrom(typeToConvert);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return Converters.GetOrAdd(typeToConvert, CreateConverterFor);
        }

        private static JsonConverter CreateConverterFor(Type typeToConvert)
        {
            var constructorInfo = typeToConvert
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Single();
            var valueType = constructorInfo.GetParameters().Single().ParameterType;
            var converterType = typeof(SingleValueObjectConverter<,>).MakeGenericType(typeToConvert, valueType);
            return (JsonConverter) Activator.CreateInstance(converterType);
        }
    }

    internal class SingleValueObjectConverter<T, TValue> : JsonConverter<T>
        where T : ISingleValueObject
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return default;
            }

            var value = JsonSerializer.Deserialize<TValue>(ref reader, options);
            return (T) Activator.CreateInstance(typeToConvert, value);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (TValue) value.GetValue(), options);
        }
    }
}
