using AnyService.Services;
using AnyService.Core;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace AnyService.EntityFramework
{
    public class EfRepository<TDomainModel> : IRepository<TDomainModel>
        where TDomainModel : class, IDomainModelBase
    {

        private readonly DbContext _dbContext;
        private readonly ILogger<EfRepository<TDomainModel>> _logger;

        private IQueryable<TDomainModel> DbSet => _dbContext.Set<TDomainModel>().AsNoTracking();
        private static readonly ConcurrentDictionary<Type, IEnumerable<PropertyInfo>> TypeProperties = new ConcurrentDictionary<Type, IEnumerable<PropertyInfo>>();
        public EfRepository(DbContext dbContext, ILogger<EfRepository<TDomainModel>> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }
        public virtual async Task<IEnumerable<TDomainModel>> GetAll(Pagination<TDomainModel> pagination)
        {
            if (pagination == null || pagination.QueryFunc == null)
                throw new ArgumentNullException(nameof(pagination));
            _logger.LogDebug("Get all with pagination: " + pagination.QueryAsString);
            pagination.Total = (ulong)DbSet.Where(pagination.QueryFunc).Count();
            _logger.LogDebug("GetAll set total to: " + pagination.Total);
            var q = DbSet;

            q.Skip((int)pagination.Offset).Take((int)pagination.PageSize);

            if (pagination.IncludeNested)
                q = IncludeNavigations(q);

            var dbRes = q
                .OrderBy(pagination.OrderBy, pagination.SortOrder == PaginationSettings.Desc)
                .Where(pagination.QueryFunc);
            await DetachEntities(q);
            var paginateTotal = dbRes.ToArray();
            _logger.LogDebug("GetAll total entities in page: " + paginateTotal.Count());
            return paginateTotal;
        }
        public virtual async Task<TDomainModel> GetById(string id)
        {
            _logger.LogDebug($"{nameof(GetById)} with id = {id}");
            var entity = await GetEntityById_Internal(id);
            if (entity == null)
                return null;
            await DetachEntities(new[] { entity });
            _logger.LogDebug($"{nameof(GetById)} result = {entity.ToJsonString()}");
            return entity;
        }
        public virtual async Task<TDomainModel> Insert(TDomainModel entity)
        {
            _logger.LogDebug($"{nameof(Insert)} with entity = {entity.ToJsonString()}");
            await _dbContext.Set<TDomainModel>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            await DetachEntities(new[] { entity });
            _logger.LogDebug($"{nameof(Insert)} result = {entity.ToJsonString()}");
            return entity;
        }
        public virtual async Task<TDomainModel> Update(TDomainModel entity)
        {
            _logger.LogDebug($"{nameof(Update)} with entity = {entity.ToJsonString()}");
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
            _logger.LogDebug($"{nameof(Update)} result = {entity.ToJsonString()}");
            return dbEntity;
        }
        public virtual async Task<TDomainModel> Delete(TDomainModel entity)
        {
            _logger.LogDebug($"{nameof(Delete)} with entity = {entity.ToJsonString()}");
            var dbEntity = await GetEntityById_Internal(entity.Id);
            if (dbEntity == null)
                return null;
            var entry = _dbContext.Remove(dbEntity);
            await _dbContext.SaveChangesAsync();
            entry.State = EntityState.Detached;
            _logger.LogDebug($"{nameof(Delete)} result = {entity.ToJsonString()}");
            return entity;
        }
        private async Task<TDomainModel> GetEntityById_Internal(string id)
        {
            var query = DbSet.Where(x => x.Id.Equals(id));
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