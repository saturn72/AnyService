using System.Threading.Tasks;

namespace AnyService.Services
{
    public interface IFactory<TKey, TValue>
    {
        Task<TValue> Get(TKey key);
    }
}