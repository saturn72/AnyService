using AnyService.Services;
using AnyService.Core;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace AnyService.EntityFramework
{
    public class EfRepository<TDomainModel> : IRepository<TDomainModel>
        where TDomainModel : class, IDomainModelBase
    {
        private readonly DbContext _dbContext;
        private IQueryable<TDomainModel> DbSet => _dbContext.Set<TDomainModel>().AsNoTracking();
        private static readonly IDictionary<Type, IEnumerable<PropertyInfo>> TypeProperties = new Dictionary<Type, IEnumerable<PropertyInfo>>();
        public EfRepository(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<TDomainModel>> GetAll(Paginate<TDomainModel> paginate)
        {
            if (paginate.Query == null)
                return await IncludeNavigations(DbSet).ToArrayAsync();

            var q = DbSet.Where(paginate.Query).AsQueryable();
            return await IncludeNavigations(q).ToArrayAsync();
        }
        public async Task<TDomainModel> GetById(string id)
        {
            var entity = await GetEntityById_Internal(id);
            if (entity == null)
                return null;
            DetachEntity(entity);
            return entity;
        }
        public async Task<TDomainModel> Insert(TDomainModel entity)
        {
            await _dbContext.Set<TDomainModel>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            DetachEntity(entity);
            return entity;
        }
        public async Task<TDomainModel> Update(TDomainModel entity)
        {
            var dbEntity = await GetEntityById_Internal(entity.Id);

            if (dbEntity == null)
                return null;

            foreach (var pInfo in GetTypePropertyInfos())
            {
                var value = pInfo.GetValue(entity);
                pInfo.SetValue(dbEntity, value);
            }
            _dbContext.Update(dbEntity);
            await _dbContext.SaveChangesAsync();
            DetachEntity(dbEntity);
            return dbEntity;
        }


        public async Task<TDomainModel> Delete(TDomainModel entity)
        {
            var dbEntity = await GetEntityById_Internal(entity.Id);
            if (dbEntity == null)
                return null;
            var entry = _dbContext.Remove(dbEntity);
            await _dbContext.SaveChangesAsync();
            entry.State = EntityState.Detached;
            return entity;
        }
        private async Task<TDomainModel> GetEntityById_Internal(string id)
        {
            var query = DbSet.Where(x => x.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase));
            return await IncludeNavigations(query).FirstOrDefaultAsync();
        }
        private void DetachEntity(TDomainModel entity)
        {
            _dbContext.Entry(entity).State = EntityState.Detached;

            foreach (var col in _dbContext.Entry(entity).Collections)
                col.EntityEntry.State = EntityState.Detached;
        }
        private static readonly ConcurrentDictionary<Type, IEnumerable<string>> NavigationPropertyNames
            = new ConcurrentDictionary<Type, IEnumerable<string>>();
        private IQueryable<TDomainModel> IncludeNavigations(IQueryable<TDomainModel> query)
        {
            var type = typeof(TDomainModel);

            if (!NavigationPropertyNames.TryGetValue(type, out IEnumerable<string> navigationPropertiesNames))
            {
                var allProperties = _dbContext.Model.FindEntityType(typeof(TDomainModel));
                navigationPropertiesNames = allProperties.GetNavigations().Select(x => x.Name).ToArray();
                NavigationPropertyNames.TryAdd(type, navigationPropertiesNames);
            }
            foreach (var name in navigationPropertiesNames)
                query = query.Include(name);
            return query;
        }
        private IEnumerable<PropertyInfo> GetTypePropertyInfos()
        {
            var type = typeof(TDomainModel);
            if (!TypeProperties.TryGetValue(type, out IEnumerable<PropertyInfo> pInfos))
            {
                pInfos = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => p.CanRead && p.CanWrite);
                TypeProperties[type] = pInfos;
            }
            return pInfos;
        }
    }
}