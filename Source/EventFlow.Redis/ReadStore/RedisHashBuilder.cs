// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using Redis.OM.Modeling;

namespace EventFlow.Redis.ReadStore;

//original source: https://github.dev/redis/redis-om-dotnet/tree/main/src/Redis.OM/Modeling

public class RedisHashBuilder : IRedisHashBuilder
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> CachedProperties = new();

    public IReadOnlyDictionary<string, string> BuildHashSet(object? obj)
    {
        if (obj is null)
            return new Dictionary<string, string>();

        var objType = obj.GetType();
        if (!CachedProperties.TryGetValue(objType, out var properties))
        {
            properties = objType.GetProperties();
            CachedProperties.TryAdd(objType, properties);
        }

        properties = properties.Where(x => x.GetValue(obj) != null).ToArray();

        var hash = new Dictionary<string, string>();
        foreach (var property in properties)
        {
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            var propertyName = property.Name;
            ExtractPropertyName(property, ref propertyName);
            if (type.IsPrimitive || type == typeof(decimal) || type == typeof(string) || type == typeof(GeoLoc) ||
                type == typeof(Ulid) || type == typeof(Guid))
            {
                var val = property.GetValue(obj);
                if (val != null)
                {
                    hash.Add(propertyName, val.ToString());
                }
            }
            else if (type.IsEnum)
            {
                var val = property.GetValue(obj);
                hash.Add(propertyName, ((int) val).ToString());
            }
            else if (type == typeof(DateTimeOffset))
            {
                var val = (DateTimeOffset) property.GetValue(obj);
                if (val != null)
                {
                    hash.Add(propertyName, val.ToString("O"));
                }
            }
            else if (type == typeof(DateTime) || type == typeof(DateTime?))
            {
                var val = (DateTime) property.GetValue(obj);
                if (val != default)
                {
                    hash.Add(propertyName, new DateTimeOffset(val).ToUnixTimeMilliseconds().ToString());
                }
            }
            else if (type == typeof(IEnumerable<>) || type.GetInterfaces()
                         .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                var e = (IEnumerable) property.GetValue(obj);
                var i = 0;
                foreach (var v in e)
                {
                    var innerType = v.GetType();
                    if (innerType.IsPrimitive || innerType == typeof(decimal) || innerType == typeof(string))
                    {
                        hash.Add($"{propertyName}[{i}]", v.ToString());
                    }
                    else
                    {
                        var subHash = BuildHashSet(v);
                        foreach (var kvp in subHash)
                        {
                            hash.Add($"{propertyName}[{i}].{kvp.Key}", kvp.Value);
                        }
                    }

                    i++;
                }
            }
            else
            {
                var subHash = BuildHashSet(property.GetValue(obj));
                if (subHash != null)
                {
                    foreach (var kvp in subHash)
                    {
                        hash.Add($"{propertyName}.{kvp.Key}", kvp.Value);
                    }
                }
            }
        }

        return hash;
    }

    private static void ExtractPropertyName(PropertyInfo property, ref string propertyName)
    {
        var fieldAttr = property.GetCustomAttributes(typeof(RedisFieldAttribute), true);
        if (fieldAttr.Any())
        {
            var rfa = (RedisFieldAttribute) fieldAttr.First();
            if (!string.IsNullOrEmpty(rfa.PropertyName))
            {
                propertyName = rfa.PropertyName;
            }
        }
    }
}