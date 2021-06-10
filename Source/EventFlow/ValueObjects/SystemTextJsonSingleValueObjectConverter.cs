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

using JetBrains.Annotations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventFlow.ValueObjects
{
    public class SystemTextJsonSingleValueObjectConverter<T, TValue> : JsonConverter<T>
        where T : SingleValueObject<TValue>
        where TValue : IComparable
    {
        private static readonly Type UnderlyingType;

        static SystemTextJsonSingleValueObjectConverter()
        {
            var type = typeof(T);
            var genarg = type.GetGenericArguments();
            UnderlyingType = typeof(TValue);
        }


        public override bool CanConvert(Type objectType)
        {
            return typeof(ISingleValueObject).GetTypeInfo().IsAssignableFrom(objectType);
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return default;
            }

            var value = JsonSerializer.Deserialize(ref reader, UnderlyingType, options);

            return (T)Activator.CreateInstance(typeToConvert, value);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.GetValue(), options);
        }
    }

    public class SystemTextJsonSingleValueObjectConverterFactory : JsonConverterFactory
    {
        private static readonly Type ConverterGenericType = typeof(SystemTextJsonSingleValueObjectConverter<,>);
        private static readonly ConcurrentDictionary<Type, JsonConverter> Converters = new ConcurrentDictionary<Type, JsonConverter>();

        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(ISingleValueObject).GetTypeInfo().IsAssignableFrom(typeToConvert);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converter = Converters.GetOrAdd(
                typeToConvert,
                t =>
                {
                    var constructedType = ConverterGenericType.MakeGenericType(typeToConvert.GetGenericArguments());
                    return Activator.CreateInstance(constructedType) as JsonConverter;
                });

            return converter;
        }
    }
}
