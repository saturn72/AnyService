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
        /// <summary>
        /// Get Parent with ALL childs
        /// </summary>
        /// <param name="parentId">parent id</param>
        /// <param name="childEntityNames">Unique entity name</param>
        /// <returns></returns>
        Task<ServiceResponse<IReadOnlyDictionary<string, IEnumerable<IDomainEntity>>>> GetAggregated(string parentId, IEnumerable<string> childEntityNames);
        /// <summary>
        /// Gets page of child elements
        /// </summary>
        /// <typeparam name="TChild">Child Entity</typeparam>
        /// <param name="parentId">parent Id</param>
        /// <param name="pagination">pagination data</param>
        /// <param name="childEntityName">Child entity name. Use this value when using multiple entity records of same type in same aggregate-root (parent)</param>
        /// <returns></returns>
        Task<ServiceResponse<Pagination<TChild>>> GetAggregatedPage<TChild>(string parentId, Pagination<TChild> pagination, string childEntityName = null)
            where TChild : IDomainEntity;

        Task<ServiceResponse<TDomainModel>> Update(string id, TDomainModel entity);
        //Task UploadFiles(IFileContainer fileContainer, ServiceResponse serviceResponse);
    }
}