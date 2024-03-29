using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public interface IGenericRepository<TDbModel, TId> where TDbModel : IDbRecord<TId>
    {
        Task<IQueryable<TDbModel>> Collection { get; }
        Task<TDbModel> Insert(TDbModel entity);
        /// <summary>
        /// Insert multiple entities in one call
        /// </summary>
        /// <param name="entities">Entities to insert</param>
        /// <param name="track">specifies if database data should be fetched after insertion. Default is false.</param>
        /// <returns></returns>
        Task<IEnumerable<TDbModel>> BulkInsert(IEnumerable<TDbModel> entities, bool track = false);
        Task<IEnumerable<TDbModel>> BulkUpdate(IEnumerable<TDbModel> entities, bool track = false);
        Task<TDbModel> GetById(TId id);
        Task<IEnumerable<TDbModel>> GetAll(Pagination<TDbModel> paginate);
        Task<TDbModel> Update(TDbModel entity);
        Task<TDbModel> Delete(TDbModel entity);
    }
    public interface IRepository<TDbModel> : IGenericRepository<TDbModel, string>
        where TDbModel : IDbRecord<string>
    {
    }
}