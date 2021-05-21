using EventFlow.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace EventFlow.Extensions
{
    public static class SystemTextJsonSerializerOptionsExtensions
    {
        public static JsonSerializerOptions AddSingleValueObjects(this JsonSerializerOptions options)
        {
            options.Converters.Add(new SystemTextJsonSingleValueObjectConverterFactory());
            return options;
        }
    }
}
