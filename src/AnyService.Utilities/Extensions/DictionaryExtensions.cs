namespace System.Collections.Generic
{
    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key, TValue defaultValue = default)
        {
            source.TryGetValue(key, out defaultValue);
            return defaultValue;
        }
    }
}
