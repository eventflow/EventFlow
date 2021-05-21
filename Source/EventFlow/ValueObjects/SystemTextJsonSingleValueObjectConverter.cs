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
