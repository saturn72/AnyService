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
            entity.Id = Guid.NewGuid().ToString() + "-" + DateTime.UtcNow.ToIso8601();
            await Task.Run(() => Command(db => db.GetCollection<TDomainModel>().Insert(entity)));
            return entity;
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
