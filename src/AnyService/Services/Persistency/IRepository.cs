using System.Collections.Generic;
using System.Threading.Tasks;
using AnyService.Core;

namespace AnyService.Services
{
    public interface IRepository<TDomainModel> where TDomainModel : IDomainModelBase
    {
        Task<TDomainModel> Insert(TDomainModel entity);
        Task<TDomainModel> GetById(string id);
        Task<IEnumerable<TDomainModel>> GetAll(Pagination<TDomainModel> paginate);
        Task<TDomainModel> Update(TDomainModel entity);
        Task<TDomainModel> Delete(TDomainModel entity);
    }
}