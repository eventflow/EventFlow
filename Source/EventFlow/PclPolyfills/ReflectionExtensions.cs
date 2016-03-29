using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EventFlow
{
    internal static class ReflectionExtensions
    {
        public static bool IsClosedTypeOf(this Type @this, Type openGeneric)
        {
            return IsClosedTypeOf(@this?.GetTypeInfo(), openGeneric);
        }

        public static bool IsClosedTypeOf(this TypeInfo @this, Type openGeneric)
        {
            if (@this == null) throw new ArgumentNullException(nameof(@this));
            if (openGeneric == null) throw new ArgumentNullException(nameof(openGeneric));

            if (!@this.ContainsGenericParameters)
                return false;

            return EnumerateTypesAssignableFrom(@this)
                .Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == openGeneric);
        }

        static IEnumerable<TypeInfo> EnumerateTypesAssignableFrom(TypeInfo typeInfo)
        {
            foreach (var t in typeInfo.ImplementedInterfaces)
            {
                yield return t.GetTypeInfo();
            }

            // Go vertical
            while (typeInfo != null)
            {
                yield return typeInfo;
                typeInfo = typeInfo.BaseType?.GetTypeInfo();
            }
        }

    }
}
