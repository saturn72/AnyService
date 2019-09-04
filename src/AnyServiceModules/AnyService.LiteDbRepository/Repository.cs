using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnyService.Services;
using LiteDB;

namespace AnyService.LiteDbRepository
{
    public class Repository<TDomainModel> : IRepository<TDomainModel> where TDomainModel : IDomainModelBase
    {
        private static readonly IDictionary<Type, string> TableNames = new Dictionary<Type, string>();
        private readonly string _dbName;

        public Repository(string dbName)
        {
            _dbName = dbName;
        }
        #region utils
        public void Command(Action<LiteDatabase> command)
        {
            using (var db = new LiteDatabase(_dbName))
            {
                command(db);
            }
        }

        public TQueryResult Query<TQueryResult>(Func<LiteDatabase, TQueryResult> query)
        {
            using (var db = new LiteDatabase(_dbName))
            {
                return query(db);
            }
        }
        #endregion
        public async Task<IEnumerable<TDomainModel>> GetAll(IDictionary<string, string> filter)
        {
            return await Task.Run(() => Query(db => db.GetCollection<TDomainModel>().FindAll()));
        }
        public async Task<TDomainModel> Insert(TDomainModel entity)
        {
            entity.Id = AssignId();
            await Task.Run(() => Command(db =>
            {
                ICollection<FileModel> fileList = new List<FileModel>();
                if (entity is IFileContainer)
                {
                    var e = (entity as IFileContainer);
                    foreach (var f in e.Files)
                    {
                        f.ContainerId = entity.Id;
                        f.Id = AssignId();

                        fileList.Add(new FileModel { Id = f.Id, FileName = f.FileName, Stream = f.Stream });
                        f.Stream = null;
                    }
                }

                db.GetCollection<TDomainModel>().Insert(entity);
                if (fileList.IsNullOrEmpty())
                    return;
                foreach (var f in fileList)
                {
                    db.FileStorage.Upload(f.Id, f.FileName, f.Stream);
                    f.Stream.Dispose();
                }
            }));
            return entity;
            string AssignId()
            {
                return DateTime.UtcNow.ToString("yyyyMMddTHHmmssK") + "-" + Guid.NewGuid().ToString();
            }
        }
        public async Task<TDomainModel> Update(TDomainModel entity)
        {
            await Task.Run(() => Command(db => db.GetCollection<TDomainModel>().Update(entity)));
            return entity;
        }
        public async Task<TDomainModel> GetById(string id)
        {
            return await Task.Run(() => Query(db => db.GetCollection<TDomainModel>().FindById(id)));
        }
    }
}
