using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace System
{
    public static class ObjectExtensionsFunctions
    {
        public static dynamic ToDynamic<T>(this T obj, IEnumerable<string> propertyNames, bool ignoreCase = true)
        {
            var stringComparison = ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.CurrentCulture;
            var comperar = ignoreCase ?
                StringComparer.InvariantCultureIgnoreCase :
                StringComparer.CurrentCulture;

            IDictionary<string, object> expando = new ExpandoObject();
            var pInfos = typeof(T).GetProperties();
            var pInfosToAdd = new Dictionary<string, PropertyInfo>(comperar);
            foreach (var pn in propertyNames)
            {
                var pi = pInfos.FirstOrDefault(p => p.Name.Equals(pn, stringComparison));
                if (pi == default) continue;
                pInfosToAdd[pn] = pi;
            }
            foreach (var pita in pInfosToAdd)
                expando[pita.Key] = pita.Value.GetValue(obj);
            return expando as ExpandoObject;
        }

        public static bool IsOfType(this Type source, Type typeToCheck) => typeToCheck.IsAssignableFrom(source);
        public static bool IsOfType<T>(this Type source) => IsOfType(source, typeof(T));
        public static IEnumerable<Type> GetAllBaseTypes(this Type type, Type excludeFromType = null)
        {
            var baseTypes = new List<Type>();
            var bt = type.BaseType;
            while (bt != excludeFromType)
            {
                baseTypes.Add(bt);
                bt = bt.BaseType;
            }
            return baseTypes;
        }
        private static readonly IDictionary<Type, IDictionary<string, PropertyInfo>> PropertyInfos = new Dictionary<Type, IDictionary<string, PropertyInfo>>();
        public static PropertyInfo GetPropertyInfo(this Type type, string propertyName)
        {
            if (!PropertyInfos.TryGetValue((Type)type, out IDictionary<string, PropertyInfo> curPropertyInfo))
            {
                curPropertyInfo = new Dictionary<string, PropertyInfo>();
                PropertyInfos[(Type)type] = curPropertyInfo;
            }
            if (!curPropertyInfo.TryGetValue(propertyName, out PropertyInfo pi))
            {
                pi = type.GetProperty(propertyName);
                if (pi != null)
                    curPropertyInfo[propertyName] = pi;
                return pi;
            }
            return null;
        }
        public static T GetPropertyValueByName<T>(this object obj, string propertyName)
        {
            var pi = obj.GetType().GetProperty(propertyName);
            if (pi != null)
                return (T)pi.GetValue(obj);
            throw new InvalidOperationException();
        }

        private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public static T GetPropertyValueOrDefaultByName<T>(this object obj, string propertyName)
        {
            var pi = obj?.GetType().GetProperty(propertyName);
            return pi != null ? (T)pi.GetValue(obj) : default;
        }
        public static string ToJsonString(this object obj) => JsonSerializer.Serialize(obj, JsonSerializerOptions);
        public static string ToJsonArrayString(this IEnumerable<byte> bytes) => $"[{string.Join(", ", bytes.ToArray())}]";
        public static T DeepClone<T>(this T source) => source.ToJsonString().ToObject<T>();
    }
}