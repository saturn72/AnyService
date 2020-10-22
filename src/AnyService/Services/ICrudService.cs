using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public interface ICrudService<TDomainEntity> where TDomainEntity : IEntity
    {
        Task<ServiceResponse<TDomainEntity>> Create(TDomainEntity entity);
        Task<ServiceResponse<TDomainEntity>> GetById(string id);
        Task<ServiceResponse<Pagination<TDomainEntity>>> GetAll(Pagination<TDomainEntity> pagination);
        Task<ServiceResponse<TDomainEntity>> Update(string id, TDomainEntity entity);
        Task<ServiceResponse<TDomainEntity>> Delete(string id);
        /// <summary>
        /// Get Parent with ALL childs
        /// </summary>
        /// <param name="parentId">parent id</param>
        /// <param name="childEntityNames">Unique entity name</param>
        /// <returns></returns>
        Task<ServiceResponse<IReadOnlyDictionary<string, IEnumerable<IEntity>>>> GetAggregated(
            string parentId,
            IEnumerable<string> childEntityNames);
        /// <summary>
        /// Gets page of child elements
        /// </summary>
        /// <typeparam name="TChild">Child Entity</typeparam>
        /// <param name="parentId">parent Id</param>
        /// <param name="pagination">pagination data</param>
        /// <param name="childEntityName">Child entity name. Use this value when using multiple entity records of same type in same aggregate-root (parent)</param>
        /// <returns></returns>
        Task<ServiceResponse<Pagination<TChild>>> GetAggregatedPage<TChild>(
            string parentId,
            Pagination<TChild> pagination,
            string childEntityName = null)
            where TChild : IEntity;
        /// <summary>
        /// Update child-parent mapping
        /// </summary>
        /// <param name="request">Mapping request details</param>
        /// <returns></returns>
        Task<ServiceResponse<EntityMappingResponse>> UpdateMappings(EntityMappingRequest request);

        //Task UploadFiles(IFileContainer fileContainer, ServiceResponse serviceResponse);
    }
}