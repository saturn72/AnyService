using System;
using System.Threading.Tasks;

namespace AnyService.Caching
{
    public interface ICacheManager
    {
        Task<TCachedObject> Get<TCachedObject>(string key);
        Task<TCachedObject> Get<TCachedObject>(string key, Func<Task<TCachedObject>> acquire, TimeSpan expiration);
        Task Clear();
        Task Set<TCachedObject>(string key, TCachedObject data, TimeSpan expiration);
        Task Remove(string key);
    }
}
