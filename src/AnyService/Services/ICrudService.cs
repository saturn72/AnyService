using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public interface ICrudService<TDomainModel> where TDomainModel : IDomainEntity
    {
        Task<ServiceResponse<TDomainModel>> Create(TDomainModel entity);
        Task<ServiceResponse<TDomainModel>> Delete(string id);
        Task<ServiceResponse<Pagination<TDomainModel>>> GetAll(Pagination<TDomainModel> pagination);
        Task<ServiceResponse<TDomainModel>> GetById(string id);
        Task<ServiceResponse<IReadOnlyDictionary<string, IEnumerable<IDomainEntity>>>> GetAggregated(string aggregateRootId, IEnumerable<string> aggregatedToFetch);
        Task<ServiceResponse<Pagination<IDomainEntity>>> GetAggregatedPage(string aggregateRootId, string aggregatedName, Pagination<IDomainEntity> pagination);
        Task<ServiceResponse<TDomainModel>> Update(string id, TDomainModel entity);
        //Task UploadFiles(IFileContainer fileContainer, ServiceResponse serviceResponse);
    }
}