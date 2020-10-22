using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public interface IRepository<TEntity> where TEntity : IEntity
    {
        Task<IQueryable<TEntity>> Collection { get; }
        Task<TEntity> Insert(TEntity entity);
        /// <summary>
        /// Insert multiple entities in one call
        /// </summary>
        /// <param name="entities">Entities to insert</param>
        /// <param name="track">specifies if database data should be fetched after insertion. Default is false.</param>
        /// <returns></returns>
        Task<IEnumerable<TEntity>> BulkInsert(IEnumerable<TEntity> entities, bool track = false);
        Task<IEnumerable<TEntity>> BulkDelete(IEnumerable<TEntity> entities, bool track = false);
        Task<TEntity> GetById(string id);
        Task<IEnumerable<TEntity>> GetAll(Pagination<TEntity> paginate);
        Task<TEntity> Update(TEntity entity);
        Task<TEntity> Delete(TEntity entity);
    }
}