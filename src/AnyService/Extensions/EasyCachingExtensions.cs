using System.Threading;
using System.Threading.Tasks;

namespace EasyCaching.Core
{
    public static class EasyCachingExtensions
    {
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
