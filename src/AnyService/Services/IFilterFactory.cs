using System;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public interface IFilterFactory
    {
        Task<Func<object, Func<TEntity, bool>>> GetFilter<TEntity>(string filterKey) 
            where TEntity : IEntity;
    }
}