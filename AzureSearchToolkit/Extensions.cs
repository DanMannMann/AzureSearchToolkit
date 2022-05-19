using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Marsman.AzureSearchToolkit
{
    internal static class Extensions
    {

        internal static ValueType GetValueType(this Type propertyType)
        {
            propertyType = propertyType.UnwrapNullable();
            return typeof(double).IsAssignableFrom(propertyType) ||
                                typeof(IEnumerable<double>).IsAssignableFrom(propertyType)
                                    ? ValueType.Numeric
                                    : typeof(DateTimeOffset).IsAssignableFrom(propertyType) ||
                                      typeof(IEnumerable<DateTimeOffset>).IsAssignableFrom(propertyType)
                                          ? ValueType.DateTime
                                          : typeof(bool).IsAssignableFrom(propertyType) ||
                                            typeof(IEnumerable<bool>).IsAssignableFrom(propertyType)
                                                ? ValueType.Boolean
                                                : ValueType.String;
        }

        internal static Type UnwrapNullable(this Type propertyType)
        {
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>)) return propertyType.GetGenericArguments()[0];
            return propertyType;
        }

        internal static IEnumerable<T> If<T>(this IEnumerable<T> backing, bool condition, Func<IEnumerable<T>, IEnumerable<T>> branch)
        {
            return condition
                    ? branch(backing)
                    : backing;
        }
        internal static IEnumerable<T> IfNotNull<T>(this IEnumerable<T> backing, object obj, Func<IEnumerable<T>, IEnumerable<T>> branch)
        {
            return obj != null
                    ? branch(backing)
                    : backing;
        }
        internal static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> backing)
        {
            return backing != null
                    ? backing
                    : new List<T>();
        }

        internal static IEnumerable<T> Except<T>(this IEnumerable<T> collection, T itemToExclude)
        {
            return itemToExclude == null ? collection.Where(x => x != null) : collection.Where(x => !itemToExclude.Equals(x));
        }
        internal static IEnumerable<T> Except<T>(this IEnumerable<T> collection, T itemToExclude, IEqualityComparer<T> equalityComparer)
        {
            return collection.Where(x => equalityComparer.Equals(x, itemToExclude));
        }

        internal static bool IsNumericType(this object o)
        {
            if (o == null) return false;
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }
    internal static class PropertyInfoExtensions
    {
        private static readonly ConcurrentDictionary<PropertyInfo, string> cache = new ConcurrentDictionary<PropertyInfo, string>();
        internal static string GetSearchFieldName(this PropertyInfo property) =>
            cache.GetOrAdd(property, p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? p.Name);

        internal static string GetDisplayName(this PropertyInfo prop)
        {
            var displayAttr = prop.GetCustomAttribute<SearchToolkitDisplayAttribute>();
            if (displayAttr?.SpaceOutPascalCase != false)
            {
                var reg = new Regex("([a-z,0-9])([A-Z])");
                var reg2 = new Regex("([a-z,A-Z])([0-9])");
                return reg2.Replace(reg.Replace(displayAttr?.DisplayName ?? prop.Name, SpacePascal), SpacePascal);
            }
            return displayAttr?.DisplayName;
        }
        private static string SpacePascal(Match match) =>
            $"{match.Groups[1].Value} {match.Groups[2].Value}";
    }
}
