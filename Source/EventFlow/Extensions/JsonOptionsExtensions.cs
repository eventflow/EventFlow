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
using System.Linq;
using EventFlow.Configuration.Serialization;
using EventFlow.ValueObjects;
using Newtonsoft.Json;

namespace EventFlow.Extensions
{
    public static class JsonOptionsExtensions
    {
        public static IJsonOptions Configure(this IJsonOptions options, Action<JsonSerializerSettings> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return new ChainedJsonOptions(options, action);
        }

        public static ChainedJsonOptions Use(this JsonOptions options, Func<JsonSerializerSettings> settingsFactory)
        {
            return new ChainedJsonOptions(options, target =>
            {
                var source = settingsFactory();
                source.CopyTo(target);
            });
        }

        public static IJsonOptions AddSingleValueObjects(this IJsonOptions options)
        {
            return options.AddConverter<SingleValueObjectConverter>();
        }

        public static IJsonOptions AddConverter<T>(this IJsonOptions options)
            where T : JsonConverter, new()
        {
            return options.Configure(s => s.Converters.Insert(0, new T()));
        }

        private static JsonSerializerSettings Clone(this JsonSerializerSettings settings)
        {
            var result = new JsonSerializerSettings();
            settings.CopyTo(result);
            return result;
        }

        private static void CopyTo(this JsonSerializerSettings settings, JsonSerializerSettings target)
        {
            target.CheckAdditionalContent = settings.CheckAdditionalContent;
            target.ConstructorHandling = settings.ConstructorHandling;
            target.Context = settings.Context;
            target.ContractResolver = settings.ContractResolver;
            target.Culture = settings.Culture;
            target.DateFormatHandling = settings.DateFormatHandling;
            target.DateFormatString = settings.DateFormatString;
            target.DateParseHandling = settings.DateParseHandling;
            target.DateTimeZoneHandling = settings.DateTimeZoneHandling;
            target.DefaultValueHandling = settings.DefaultValueHandling;
            target.EqualityComparer = settings.EqualityComparer;
            target.Error = settings.Error;
            target.FloatFormatHandling = settings.FloatFormatHandling;
            target.FloatParseHandling = settings.FloatParseHandling;
            target.Formatting = settings.Formatting;
            target.MaxDepth = settings.MaxDepth;
            target.MetadataPropertyHandling = settings.MetadataPropertyHandling;
            target.MissingMemberHandling = settings.MissingMemberHandling;
            target.NullValueHandling = settings.NullValueHandling;
            target.ObjectCreationHandling = settings.ObjectCreationHandling;
            target.PreserveReferencesHandling = settings.PreserveReferencesHandling;
            target.ReferenceLoopHandling = settings.ReferenceLoopHandling;
            target.ReferenceResolverProvider = settings.ReferenceResolverProvider;
            target.SerializationBinder = settings.SerializationBinder;
            target.StringEscapeHandling = settings.StringEscapeHandling;
            target.TraceWriter = settings.TraceWriter;
            target.TypeNameAssemblyFormatHandling = settings.TypeNameAssemblyFormatHandling;
            target.TypeNameHandling = settings.TypeNameHandling;
            target.Converters = settings.Converters.ToList();
        }
    }
}
