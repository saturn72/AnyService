using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Caching
{
    public static class CacheManagerExtensions
    {
        public static async Task<IEnumerable<T>> ToCachedEnumerable<T>(
            this ICacheManager cacheManager,
            Task<IQueryable<T>> query,
            string cacheKey,
            TimeSpan expiration = default)
        {
            if (!cacheKey.HasValue())
                return (await query).ToArray();

            return await cacheManager.Get(cacheKey, async () => (await query).ToList().AsEnumerable(), expiration);
        }

        public async static Task<IEnumerable<T>> ToCachedEnumerable<T>(this ICacheManager cacheManager, IQueryable<T> query, string cacheKey, TimeSpan expiration = default)
        {
            if (!cacheKey.HasValue())
                return query.ToArray();

            return await cacheManager.Get(
                cacheKey,
                () => Task.FromResult(query.ToList().AsEnumerable()),
                expiration);
        }
        public static async Task<ICollection<T>> ToCachedCollection<T>(this ICacheManager cacheManager, Task<IQueryable<T>> query, string cacheKey, TimeSpan expiration = default)
        {
            if (!cacheKey.HasValue())
                return (await query).ToList();

            return await cacheManager.Get(cacheKey, async () => (await query).ToList() as ICollection<T>, expiration);
        }

        public static async Task<ICollection<T>> ToCachedCollection<T>(this ICacheManager cacheManager, IQueryable<T> query, string cacheKey, TimeSpan expiration = default)
        {
            if (!cacheKey.HasValue())
                return query.ToList();

            return await cacheManager
                .Get(
                    cacheKey,
                    () => Task.FromResult(query.ToList() as ICollection<T>),
                    expiration);
        }
    }
}
