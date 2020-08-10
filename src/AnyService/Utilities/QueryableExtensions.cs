using System.Collections.Generic;
using System.Threading.Tasks;
using AnyService;
using AnyService.Caching;

namespace System.Linq
{
    public static class QueryableExtensions
    {
        private static ICacheManager CacheManagerResolver() => AppEngine.GetService<ICacheManager>();
        public static async Task<IEnumerable<T>> ToCachedEnumerable<T>(this Task<IQueryable<T>> query, string cacheKey, TimeSpan expiration = default)
        {
            if (!cacheKey.HasValue())
                return (await query).ToArray();

            return await CacheManagerResolver().Get(cacheKey, async () => (await query).ToList().AsEnumerable(), expiration);
        }

        public async static Task<IEnumerable<T>> ToCachedEnumerable<T>(this IQueryable<T> query, string cacheKey, TimeSpan expiration = default)
        {
            if (!cacheKey.HasValue())
                return query.ToArray();

            return await CacheManagerResolver().Get(
                cacheKey,
                () => Task.FromResult(query.ToList().AsEnumerable()),
                expiration);
        }
        public static async Task<ICollection<T>> ToCachedCollection<T>(this Task<IQueryable<T>> query, string cacheKey, TimeSpan expiration = default)
        {
            if (!cacheKey.HasValue())
                return (await query).ToList();

            return await CacheManagerResolver().Get(cacheKey, async () => (await query).ToList() as ICollection<T>, expiration);
        }

        public static async Task<ICollection<T>> ToCachedCollection<T>(this IQueryable<T> query, string cacheKey, TimeSpan expiration = default)
        {
            if (!cacheKey.HasValue())
                return query.ToList();

            return await CacheManagerResolver()
                .Get(
                    cacheKey,
                    () => Task.FromResult(query.ToList() as ICollection<T>),
                    expiration);
        }
    }
}