using System.Collections.Generic;

namespace System.Text.Json
{
    public static class JsonExtensions
    {
        public static T ToObject<T>(this JsonElement jsonElement)
        {
            return StringExtensions.ToObject<T>(jsonElement.ToString());
        }
        public static object ToObject(this JsonElement jsonElement, Type toType)
        {
            return StringExtensions.ToObject(jsonElement.ToString(), toType);
        }
        public static JsonElement FirstElementOrDefault(this JsonElement jElem, Func<JsonElement, bool> exp)
        {
            foreach (var je in jElem.EnumerateArray())
                if (exp(je)) return je;
            return default;
        }
        private static readonly IDictionary<Type, Func<JsonElement, object>> JsonValueConvert = new Dictionary<Type, Func<JsonElement, object>>
        {
            {typeof(bool), je => je.GetBoolean()},
            {typeof(byte), je => je.GetByte()},
            {typeof(DateTime), je => je.GetDateTime()},
            {typeof(DateTimeOffset), je => je.GetDateTimeOffset()},
            {typeof(decimal), je => je.GetDecimal()},
            {typeof(double), je => je.GetDouble()},
            {typeof(Guid), je => je.GetGuid()},
            {typeof(short), je => je.GetInt16()},
            {typeof(int), je => je.GetInt32()},
            {typeof(long), je => je.GetInt64()},
            {typeof(sbyte), je => je.GetSByte()},
            {typeof(string), je => je.GetString()},
            {typeof(ushort), je => je.GetUInt16()},
            {typeof(uint), je => je.GetUInt32()},
            {typeof(ulong), je => je.GetUInt64()},
        };
        public static T GetValue<T>(this JsonElement jsonElement, string propertyName)
        {
            return (T)GetValue(jsonElement, typeof(T), propertyName);
        }
        public static object GetValue(this JsonElement jsonElement, Type type, string propertyName)
        {
            if (!jsonElement.TryGetProperty(propertyName, out JsonElement value))
                return default;
            return JsonValueConvert[type](value);
        }
    }
}
