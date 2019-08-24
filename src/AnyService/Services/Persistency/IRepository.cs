using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public interface IRepository<TDomainModel> where TDomainModel : IDomainModelBase
    {
        Task<TDomainModel> Insert(TDomainModel entity);
        Task<TDomainModel> GetById(string id);
        Task<IEnumerable<TDomainModel>> GetAll(IDictionary<string, string> filter = null);
        Task<TDomainModel> Update(TDomainModel entity);
    }
}