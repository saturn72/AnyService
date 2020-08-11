using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace AnyService.Caching
{
    public static class QueryableExtensions
    {
        public static void Init(IServiceProvider serviceProvider) {
            _cacheManagerResolver = () => serviceProvider.GetService<ICacheManager>();
        }
        private static Func<ICacheManager> _cacheManagerResolver;
        public static async Task<IEnumerable<T>> ToCachedEnumerable<T>(this Task<IQueryable<T>> query, string cacheKey, TimeSpan expiration = default)
        {
            if (!cacheKey.HasValue())
                return (await query).ToArray();

            return await _cacheManagerResolver().Get(cacheKey, async () => (await query).ToList().AsEnumerable(), expiration);
        }

        public async static Task<IEnumerable<T>> ToCachedEnumerable<T>(this IQueryable<T> query, string cacheKey, TimeSpan expiration = default)
        {
            if (!cacheKey.HasValue())
                return query.ToArray();

            return await _cacheManagerResolver().Get(
                cacheKey,
                () => Task.FromResult(query.ToList().AsEnumerable()),
                expiration);
        }
        public static async Task<ICollection<T>> ToCachedCollection<T>(this Task<IQueryable<T>> query, string cacheKey, TimeSpan expiration = default)
        {
            if (!cacheKey.HasValue())
                return (await query).ToList();

            return await _cacheManagerResolver().Get(cacheKey, async () => (await query).ToList() as ICollection<T>, expiration);
        }

        public static async Task<ICollection<T>> ToCachedCollection<T>(this IQueryable<T> query, string cacheKey, TimeSpan expiration = default)
        {
            if (!cacheKey.HasValue())
                return query.ToList();

            return await _cacheManagerResolver()
                .Get(
                    cacheKey,
                    () => Task.FromResult(query.ToList() as ICollection<T>),
                    expiration);
        }
    }
}