using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace System
{
    public static class StringExtensions
    {
        public static IEnumerable<string> FromDelimitedString(this string input, string delimiter)
        {
            if (input == default)
                return default;
            if (!delimiter.HasValue())
                return new[] { input };

            input = input.Trim();
            return input.Split(delimiter, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public static string ToDelimitedString(this IEnumerable<string> source, string delimiter = "")
        {
            if (source == null)
                return null;

            if (delimiter == null) delimiter = string.Empty;

            var len = source.Count();
            if (len == 0)
                return string.Empty;

            var sb = new StringBuilder(100);
            var i = 0;
            while (i < len - 1)
            {
                sb.Append(source.ElementAt(i) + delimiter);
                i++;
            }
            sb.Append(source.ElementAt(i));

            return sb.ToString();
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