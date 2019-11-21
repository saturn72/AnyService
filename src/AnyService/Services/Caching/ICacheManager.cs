using System;
using System.Threading.Tasks;

namespace AnyService.Services.Caching
{
    public interface ICacheManager
    {
        Task<TCached> GetAsync<TCached>(string key, Func<Task<TCached>> acquire, TimeSpan expiration);
    }
}
