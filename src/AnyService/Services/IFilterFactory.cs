using System;
using AnyService.Core;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public interface IFilterFactory
    {
        Func<object, Task<Func<TDomainModel, bool>>> GetFilter<TDomainModel>(string filterKey) where TDomainModel : IDomainModelBase;
    }
}