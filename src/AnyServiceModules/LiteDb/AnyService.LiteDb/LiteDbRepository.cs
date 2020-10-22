using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnyService.Services;
using AnyService.Services.FileStorage;
using LiteDB;

namespace AnyService.LiteDb
{
    public class LiteDbRepository<TDomainEntity> :
        IRepository<TDomainEntity> where TDomainEntity : IDomainEntity
    {
        private readonly string _dbName;

        public LiteDbRepository(string dbName)
        {
            _dbName = dbName;


        }
        public Task<IQueryable<TDomainEntity>> Collection => Task.FromResult(LiteDbUtility.Collection<TDomainEntity>(_dbName));

        public async Task<TDomainEntity> Insert(TDomainEntity entity)
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
                db.GetCollection<TDomainEntity>().Insert(entity);
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
        public async Task<IEnumerable<TDomainEntity>> BulkInsert(IEnumerable<TDomainEntity> entities, bool trackIds = false)
        {
            foreach (var e in entities)
                e.Id = AssignId();
            await Task.Run(() => LiteDbUtility.Command(_dbName, db => db.GetCollection<TDomainEntity>().InsertBulk(entities)));
            return entities;
        }

        public Task<IEnumerable<TDomainEntity>> GetAll(Pagination<TDomainEntity> pagination)
        {
            var data = LiteDbUtility.Query<IEnumerable<TDomainEntity>>(_dbName, db =>
            {
                var col = db.GetCollection<TDomainEntity>();
                if (pagination == null)
                    return col.FindAll().ToArray();

                var query = ExpressionTreeBuilder.BuildBinaryTreeExpression<TDomainEntity>(pagination.QueryOrFilter);
                if (query == null)
                    return null;
                return col.Find(query).ToArray();
            });
            return Task.FromResult(data);
        }
        public async Task<TDomainEntity> GetById(string id)
        {
            return await Task.Run(() => LiteDbUtility.Query(_dbName, db => db.GetCollection<TDomainEntity>().FindById(id)));
        }
        public async Task<TDomainEntity> Update(TDomainEntity entity)
        {
            await Task.Run(() => LiteDbUtility.Command(_dbName, db => db.GetCollection<TDomainEntity>().Update(entity)));
            return entity;
        }
        public async Task<TDomainEntity> Delete(TDomainEntity entity)
        {
            await Task.Run(() =>
                LiteDbUtility.Command(_dbName, db =>
                {
                    var col = db.GetCollection<TDomainEntity>();
                    if (!col.Delete(entity.Id))
                        entity = default(TDomainEntity);
                }));
            return entity;
        }
    }
}
