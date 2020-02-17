using System;
using System.Threading.Tasks;
using AnyService.Core.Caching;
using EasyCaching.Core;

namespace AnyService.EasyCaching
{
    public class EasyCachingCacheManager : ICacheManager
    {
        private readonly IEasyCachingProvider _provider;
        private readonly uint _defaultCachingTime;

        public EasyCachingCacheManager(IEasyCachingProvider provider, EasyCachingConfig config)
        {
            _provider = provider;
            _defaultCachingTime = config.DefaultCachingTimeInSeconds;
        }
        public Task Set<TCachedObject>(string key, TCachedObject data, TimeSpan expiration = default) =>
            _provider.SetAsync(key, data, ExtractExpirationOrDefault(expiration));
        public async Task<TCachedObject> Get<TCachedObject>(string key) =>
            (await _provider.GetAsync<TCachedObject>(key)).Value;
        public async Task<TCachedObject> Get<TCachedObject>(string key, Func<Task<TCachedObject>> acquire, TimeSpan expiration = default) =>
            (await _provider.GetAsync(key, acquire, ExtractExpirationOrDefault(expiration))).Value;
        public Task Remove(string cacheKey) => _provider.RemoveAsync(cacheKey);
        public Task Clear() => _provider.FlushAsync();

        #region Utilities
        private TimeSpan ExtractExpirationOrDefault(TimeSpan expiration) => expiration.Equals(new TimeSpan()) ? TimeSpan.FromSeconds(_defaultCachingTime) : expiration;

        #endregion
    }
}
