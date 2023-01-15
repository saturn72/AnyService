using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Distributed
{
    public static class DistributedCacheExtensionsClass
    {
        public static async Task<T> GetAsync<T>(
          this IDistributedCache cache,
          string key,
          CancellationToken token = default)
        {
            var value = await cache.GetAsync(key, token);
            if (value == default)
                return default;

            return JsonSerializer.Deserialize<T>(value) ?? throw new ArgumentNullException(key);
        }
        public static async Task<T> GetAsync<T>(
               this IDistributedCache cache,
               string key,
               Func<Task<T>> acquirar,
               TimeSpan absoluteExpirationRelativeToNow,
               CancellationToken token = default)
        {
            var value = await cache.GetAsync(key, token);

            if (value == default)
            {
                var t = await acquirar();
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow
                };

                value = JsonSerializer.SerializeToUtf8Bytes(t);
                await cache.SetAsync(key, value, options, token);
            }

            return JsonSerializer.Deserialize<T>(value);
        }
    }
}
