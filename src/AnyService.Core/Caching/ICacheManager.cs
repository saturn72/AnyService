using System;
using System.Threading.Tasks;

namespace AnyService.Core.Caching
{
    public interface ICacheManager
    {
        Task<TCachedObject> GetAsync<TCachedObject>(string key);
        Task<TCachedObject> GetAsync<TCachedObject>(string key, Func<Task<TCachedObject>> acquire, TimeSpan expiration);
        Task Clear();
        Task SetAsync<TCachedObject>(string key, TCachedObject data, TimeSpan expiration);
        Task Remove(string key);
    }
}
