using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EasyCaching.Core
{
    public static class EasyCachingExtensions
    {
        public static async Task<T> GetValueAsync<T>(this IEasyCachingProvider cache,
            string cacheKey,
            Func<Task<T>> dataRetriever,
            TimeSpan expiration,
            CancellationToken cancellationToken = default)
        {
            var d = await cache.GetAsync(cacheKey, dataRetriever, expiration, cancellationToken);
            return d.HasValue ? d.Value : default;
        }

        public static async Task<IEnumerable<T>> GetValueByPrefixAsync<T>(this IEasyCachingProvider cache, string prefix, CancellationToken cancellationToken = default)
        {
            var d = await cache.GetByPrefixAsync<T>(prefix, cancellationToken);
            return d?.Select(c => c.Value.Value) ?? Array.Empty<T>();
        }

        public static async Task<T> GetDefaultAsync<T>(this IEasyCachingProvider cache, string cacheKey, T defaultValue = default, CancellationToken cancellationToken = default)
        {
            var cv = await cache.GetAsync<T>(cacheKey, cancellationToken);
            return cv?.HasValue == true ? cv.Value : defaultValue;
        }
        public static async Task<(bool hasValue, T value)> TryGetAsync<T>(this IEasyCachingProvider cache, string cacheKey, CancellationToken cancellationToken = default)
        {
            var cv = await cache.GetAsync<T>(cacheKey, cancellationToken);
            return cv?.HasValue == true ? (true, cv.Value) : (false, default);
        }
    }
}
