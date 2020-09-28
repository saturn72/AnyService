using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public interface IRepository<TDomainModel> where TDomainModel : IDomainObject
    {
        Task<IQueryable<TDomainModel>> Collection { get; }
        Task<TDomainModel> Insert(TDomainModel entity);
        /// <summary>
        /// Insert multiple entities in one call
        /// </summary>
        /// <param name="entities">Entities to insert</param>
        /// <param name="track">specifies if database data should be fetched after insertion. Default is false.</param>
        /// <returns></returns>
        Task<IEnumerable<TDomainModel>> BulkInsert(IEnumerable<TDomainModel> entities, bool track = false);
        Task<TDomainModel> GetById(string id);
        Task<IEnumerable<TDomainModel>> GetAll(Pagination<TDomainModel> paginate);
        Task<TDomainModel> Update(TDomainModel entity);
        Task<TDomainModel> Delete(TDomainModel entity);
    }
}