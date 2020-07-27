using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public interface IRepository<TDomainModel> where TDomainModel : IDomainModelBase
    {
        IQueryable<TDomainModel> Collection { get; }
        Task<TDomainModel> Insert(TDomainModel entity);
        Task<TDomainModel> GetById(string id);
        Task<IEnumerable<TDomainModel>> GetAll(Pagination<TDomainModel> paginate);
        Task<TDomainModel> Update(TDomainModel entity);
        Task<TDomainModel> Delete(TDomainModel entity);
    }
}