using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnyService.Services;
using AnyService.Services.FileStorage;
using LiteDB;

namespace AnyService.LiteDb
{
    public class LiteDbRepository<TEntity> :
        IRepository<TEntity> where TEntity : IDbModel<string>
    {
        private readonly string _dbName;

        public LiteDbRepository(string dbName)
        {
            _dbName = dbName;


        }
        public Task<IQueryable<TEntity>> Collection => Task.FromResult(LiteDbUtility.Collection<TEntity>(_dbName));

        public async Task<TEntity> Insert(TEntity entity)
        {
            entity.Id = AssignId();
            await Task.Run(() => LiteDbUtility.Command(_dbName, db =>
            {
                ICollection<FileModel> fileList = new List<FileModel>();
                var isFileContainer = entity is IFileContainer;
                if (isFileContainer)
                {
                    foreach (var f in (entity as IFileContainer).Files)
                    {
                        f.ParentId = entity.Id;
                        f.Id = AssignId();

                        fileList.Add(new FileModel { Id = f.Id, DisplayFileName = f.DisplayFileName, Bytes = f.Bytes });
                        f.Bytes = null;
                    }
                }
                db.GetCollection<TEntity>().Insert(entity);
                if (isFileContainer)
                {
                    foreach (var f in (entity as IFileContainer).Files)
                        f.Bytes = fileList.First(i => i.Id == f.Id).Bytes;
                }
            }));
            return entity;
        }
        private static string AssignId()
        {
            return DateTime.UtcNow.ToString("yyyyMMddTHHmmssK") + "-" + Guid.NewGuid().ToString();
        }
        public async Task<IEnumerable<TEntity>> BulkInsert(IEnumerable<TEntity> entities, bool trackIds = false)
        {
            foreach (var e in entities)
                e.Id = AssignId();
            await Task.Run(() => LiteDbUtility.Command(_dbName, db => db.GetCollection<TEntity>().InsertBulk(entities)));
            return entities;
        }

        public Task<IEnumerable<TEntity>> GetAll(Pagination<TEntity> pagination)
        {
            var data = LiteDbUtility.Query<IEnumerable<TEntity>>(_dbName, db =>
            {
                var col = db.GetCollection<TEntity>();
                if (pagination == null)
                    return col.FindAll().ToArray();

                var query = ExpressionTreeBuilder.BuildBinaryTreeExpression<TEntity>(pagination.QueryOrFilter);
                if (query == null)
                    return null;
                return col.Find(query).ToArray();
            });
            return Task.FromResult(data);
        }
        public async Task<TEntity> GetById(string id)
        {
            return await Task.Run(() => LiteDbUtility.Query(_dbName, db => db.GetCollection<TEntity>().FindById(id.ToString())));
        }
        public async Task<TEntity> Update(TEntity entity)
        {
            await Task.Run(() => LiteDbUtility.Command(_dbName, db => db.GetCollection<TEntity>().Update(entity)));
            return entity;
        }
        public async Task<TEntity> Delete(TEntity entity)
        {
            await Task.Run(() =>
                LiteDbUtility.Command(_dbName, db =>
                {
                    var col = db.GetCollection<TEntity>();
                    if (!col.Delete(entity.Id.ToString()))
                        entity = default;
                }));
            return entity;
        }
    }
}
