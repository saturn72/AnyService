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
        private static readonly ConcurrentDictionary<Type, IEnumerable<PropertyInfo>> TypeProperties = new ConcurrentDictionary<Type, IEnumerable<PropertyInfo>>();
        public EfRepository(DbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<IEnumerable<TDomainModel>> GetAll(Pagination<TDomainModel> pagination)
        {
            if (pagination == null || pagination.QueryFunc == null)
                throw new ArgumentNullException(nameof(pagination));
            pagination.Total = (ulong)DbSet.Count();
            var q = DbSet.Where(pagination.QueryFunc);

            var pInfo = typeof(TDomainModel).GetPropertyInfo(pagination.OrderBy);
            if (pagination.SortOrder == PaginationSettings.Asc)
                q.OrderBy(pi => pInfo.GetValue(pi, null));
            else q.OrderByDescending(pi => pInfo.GetValue(pi, null));

            q.Skip((int)pagination.Offset).Take((int)pagination.PageSize);
            var navs = IncludeNavigations(q);
            var res = await navs.ToArrayAsync();
            await DetachEntities(res);
            return res;
        }
        public async Task<TDomainModel> GetById(string id)
        {
            var entity = await GetEntityById_Internal(id);
            if (entity == null)
                return null;
            await DetachEntities(new[] { entity });
            return entity;
        }
        public async Task<TDomainModel> Insert(TDomainModel entity)
        {
            await _dbContext.Set<TDomainModel>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            await DetachEntities(new[] { entity });
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
            await DetachEntities(new[] { dbEntity });
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
        private Task DetachEntities(IEnumerable<TDomainModel> entities)
        {
            return Task.Run(() =>
            {
                foreach (var e in entities)
                {
                    _dbContext.Entry(e).State = EntityState.Detached;
                    foreach (var col in _dbContext.Entry(e).Collections)
                        col.EntityEntry.State = EntityState.Detached;
                }
            });
        }
        private static readonly ConcurrentDictionary<Type, IEnumerable<string>> NavigationPropertyNames
            = new ConcurrentDictionary<Type, IEnumerable<string>>();
        private IQueryable<TDomainModel> IncludeNavigations(IEnumerable<TDomainModel> source)
        {
            var type = typeof(TDomainModel);

            if (!NavigationPropertyNames.TryGetValue(type, out IEnumerable<string> navigationPropertiesNames))
            {
                var allProperties = _dbContext.Model.FindEntityType(typeof(TDomainModel));
                navigationPropertiesNames = allProperties.GetNavigations().Select(x => x.Name).ToArray();
                NavigationPropertyNames.TryAdd(type, navigationPropertiesNames);
            }
            foreach (var name in navigationPropertiesNames)
                source = source.AsQueryable().Include(name);
            return source.AsQueryable();
        }
        private IEnumerable<PropertyInfo> GetTypePropertyInfos()
        {
            var type = typeof(TDomainModel);
            if (!TypeProperties.TryGetValue(type, out IEnumerable<PropertyInfo> pInfos))
            {
                pInfos = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => p.CanRead && p.CanWrite);
                TypeProperties.TryAdd(type, pInfos);
            }
            return pInfos;
        }
    }
}