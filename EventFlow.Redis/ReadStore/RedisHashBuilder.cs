using System.Reflection;
using Redis.OM.Modeling;
using StackExchange.Redis;

namespace EventFlow.Redis.ReadStore;

// https://github.dev/redis/redis-om-dotnet/tree/main/src/Redis.OM/Modeling

public class RedisHashBuilder : IRedisHashBuilder
{
    public Dictionary<string, string> BuildHashSet(object obj)
    {
        if (obj is null)
            return new Dictionary<string, string>();

        var properties = obj
            .GetType()
            .GetProperties()
            .Where(x => x.GetValue(obj) != null);

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
            else if (type.GetInterfaces()
                     .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                var e = (IEnumerable<object>) property.GetValue(obj);
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
                            hash.Add($"{propertyName}.[{i}].{kvp.Key}", kvp.Value);
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