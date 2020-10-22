using System.Threading.Tasks;

namespace AnyService.Services
{
    public interface ICrudService<TEntity> where TEntity : IEntity
    {
        Task<ServiceResponse<TEntity>> Create(TEntity entity);
        Task<ServiceResponse<TEntity>> Delete(string id);
        Task<ServiceResponse<Pagination<TEntity>>> GetAll(Pagination<TEntity> pagination);
        Task<ServiceResponse<TEntity>> GetById(string id);
        Task<ServiceResponse<TEntity>> Update(string id, TEntity entity);
        //Task UploadFiles(IFileContainer fileContainer, ServiceResponse serviceResponse);
    }
}