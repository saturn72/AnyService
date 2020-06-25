using AnyService.Services.FileStorage;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public interface ICrudService<TDomainModel> where TDomainModel : IDomainModelBase
    {
        Task<ServiceResponse> Create(TDomainModel entity);
        Task<ServiceResponse> Delete(string id);
        Task<ServiceResponse> GetAll(Pagination<TDomainModel> pagination);
        Task<ServiceResponse> GetById(string id);
        Task<ServiceResponse> Update(string id, TDomainModel entity);
        Task UploadFiles(IFileContainer fileContainer, ServiceResponse serviceResponse);
    }
}