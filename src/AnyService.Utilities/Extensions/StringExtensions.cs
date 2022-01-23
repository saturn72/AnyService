using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace System
{
    public static class StringExtensions
    {
        public static string ToDelimitedString(this IEnumerable<string> source, string delimiter = "")
        {
            if (source == null)
                return null;

            if (delimiter == null) delimiter = string.Empty;

            if (source.Count() == 0)
                return string.Empty;

            var sb = new StringBuilder(100);
            foreach (var element in source)
                sb.Append(element + delimiter);
            return sb.ToString().Trim();
        }
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