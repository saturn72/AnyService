using System.Collections.Generic;
using System.Threading.Tasks;
using AnyService;
using AnyService.Caching;

namespace System.Linq
{
    public static class QueryableExtensions
    {
        public static async Task<IEnumerable<T>> ToCachedCollection<T>(this IQueryable<T> query, string cacheKey, TimeSpan expiration = default)
        {
            if (!cacheKey.HasValue())
                return query.ToArray();

            return await AppEngine.GetService<ICacheManager>().Get(cacheKey, () => Task.FromResult(query.ToArray().AsEnumerable()), expiration);
        }
    }
}