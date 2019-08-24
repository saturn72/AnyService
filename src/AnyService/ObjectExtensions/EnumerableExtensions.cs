using System.Linq;

namespace System.Collections
{
    public static class EnumerableExtensions
    {
        public static void ForEachItem(this IEnumerable collection, Action<object> action)
        {
            foreach (var item in collection)
                action(item);
        }
        public static bool IsNullOrEmpty(this IEnumerable collection)
        {
            return collection == null || !collection.GetEnumerator().MoveNext();
        }
    }
}

namespace System.Collections.Generic
{
    public static class EnumerableExtensions
    {
        public static void ForEachItem<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var item in collection)
                action(item);
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
        {
            return collection == null || !collection.Any();
        }
    }
}