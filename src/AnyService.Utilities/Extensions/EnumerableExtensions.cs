using System.Linq;

namespace System.Collections.Generic
{
    public static class EnumerableExtensions
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            return source == null || !source.Any();
        }
        public static bool HasMinLengthOf<T>(this IEnumerable<T> source, int minLength)
        {
            return source.Count() >= minLength;
        }
        public static bool HasMaxLengthOf<T>(this IEnumerable<T> source, int maxLength)
        {
            return source.Count() <= maxLength;
        }
    }
}