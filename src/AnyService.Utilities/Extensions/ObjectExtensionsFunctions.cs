using System.Collections.Generic;
using System.Reflection;

namespace System
{
    public static class ObjectExtensionsFunctions
    {
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

        public static T GetPropertyValueOrDefaultByName<T>(this object obj, string propertyName)
        {
            var pi = obj.GetType().GetProperty(propertyName);
            return pi != null ? (T)pi.GetValue(obj) : default(T);
        }
    }
}