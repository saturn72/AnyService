using System.Text.Json;

namespace System
{
    public static class StringExtensions
    {
        public static bool HasValue(this string source)
        {
            return !string.IsNullOrEmpty(source) && !string.IsNullOrWhiteSpace(source);
        }
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
        };
        public static T ToObject<T>(this string json)
        {
            return JsonSerializer.Deserialize<T>(json, SerializerOptions);
        }
        public static object ToObject(this string json, Type toType)
        {
            return JsonSerializer.Deserialize(json, toType, SerializerOptions);
        }
    }
}