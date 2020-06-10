using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnyService.Services;
using AnyService.Services.FileStorage;

namespace AnyService.LiteDb
{
    public class LiteDbRepository<TDomainModel> :
        IRepository<TDomainModel> where TDomainModel : IDomainModelBase
    {
        private static readonly IDictionary<Type, string> TableNames = new Dictionary<Type, string>();
        private readonly string _dbName;

        public LiteDbRepository(string dbName)
        {
            _dbName = dbName;
        }
        public async Task<TDomainModel> Insert(TDomainModel entity)
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
                db.GetCollection<TDomainModel>().Insert(entity);
                if (isFileContainer)
                {
                    foreach (var f in (entity as IFileContainer).Files)
                        f.Bytes = fileList.First(i => i.Id == f.Id).Bytes;
                }
            }));
            return entity;
            string AssignId()
            {
                return DateTime.UtcNow.ToString("yyyyMMddTHHmmssK") + "-" + Guid.NewGuid().ToString();
            }
        }
        public Task<IEnumerable<TDomainModel>> GetAll(Pagination<TDomainModel> pagination)
        {
            var data = LiteDbUtility.Query<IEnumerable<TDomainModel>>(_dbName, db =>
            {
                var col = db.GetCollection<TDomainModel>();
                if (pagination == null)
                    return col.FindAll().ToArray();

                var query = ExpressionTreeBuilder.BuildBinaryTreeExpression<TDomainModel>(pagination.QueryAsString);
                if (query == null)
                    return null;
                return col.Find(query).ToArray();
            });
            return Task.FromResult(data);
        }
        public async Task<TDomainModel> GetById(string id)
        {
            return await Task.Run(() => LiteDbUtility.Query(_dbName, db => db.GetCollection<TDomainModel>().FindById(id)));
        }
        public async Task<TDomainModel> Update(TDomainModel entity)
        {
            await Task.Run(() => LiteDbUtility.Command(_dbName, db => db.GetCollection<TDomainModel>().Update(entity)));
            return entity;
        }
        public async Task<TDomainModel> Delete(TDomainModel entity)
        {
            await Task.Run(() =>
                LiteDbUtility.Command(_dbName, db =>
                {
                    var col = db.GetCollection<TDomainModel>();
                    if (!col.Delete(entity.Id))
                        entity = default(TDomainModel);
                }));
            return entity;
        }
    }
}
