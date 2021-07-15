using AnyService.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Dynamic.Core;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using System.Data;
using Microsoft.Extensions.DependencyInjection;

namespace AnyService.EntityFramework
{
    public class EfGenericRepository<TDbRecord, TId> : IGenericRepository<TDbRecord, TId>
    where TDbRecord : class, IDbRecord<TId>

    {
        #region fields
        private static readonly ConcurrentDictionary<Type, IEnumerable<PropertyInfo>> TypeProperties = new ConcurrentDictionary<Type, IEnumerable<PropertyInfo>>();
        private static Func<string, string, bool> OrderByPropertyGetFunction;

        private readonly DbContext _dbContext;
        private readonly EfRepositoryConfig _config;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EfGenericRepository<TDbRecord, TId>> _logger;
        private readonly IQueryable<TDbRecord> _collection;
        #endregion
        #region ctor
        public EfGenericRepository(
            DbContext dbContext,
            EfRepositoryConfig config,
            IServiceProvider serviceProvider,
            ILogger<EfGenericRepository<TDbRecord, TId>> logger)
        {
            _dbContext = dbContext;
            _config = config;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _collection = _dbContext.Set<TDbRecord>().AsNoTracking();
            OrderByPropertyGetFunction ??= BuildOrderByPropertyMethod(_config.CaseSensitiveOrderBy);
        }
        protected Func<string, string, bool> BuildOrderByPropertyMethod(bool caseSensitiveOrderBy)
        {
            var comparison = caseSensitiveOrderBy ?
                StringComparison.CurrentCulture :
                StringComparison.InvariantCultureIgnoreCase;

            return (piName, propertyName) => piName.Equals(propertyName, comparison);
        }
        #endregion
        public Task<IQueryable<TDbRecord>> Collection => Task.FromResult(_collection);
        public virtual Task<IEnumerable<TDbRecord>> GetAll(Pagination<TDbRecord> pagination)
        {
            if (pagination == null || pagination.QueryFunc == null)
                throw new ArgumentNullException(nameof(pagination));
            _logger.LogDebug(EfRepositoryEventIds.Read, $"{nameof(GetAll)} with pagination: " + pagination.QueryOrFilter ?? pagination.QueryFunc.ToString());
            pagination.Total = _collection.Where(pagination.QueryFunc).Count();
            _logger.LogDebug(EfRepositoryEventIds.Read, $"{nameof(GetAll)} set total to: " + pagination.Total);

            var q = pagination.IncludeNested ? IncludeNavigations(_collection) : _collection;
            var isDesc = pagination.SortOrder == PaginationSettings.Desc;
            var orderByPropertyName = GetOrderByProperty(pagination.OrderBy);
            q = q.OrderBy(orderByPropertyName, isDesc)
                .Where(pagination.QueryFunc).AsQueryable();

            q = pagination.Offset == default ?
               q.Take(pagination.PageSize) :
               q.Skip(pagination.Offset).Take(pagination.PageSize);

            if (!pagination.ProjectedFields.IsNullOrEmpty())
            {
                var toProject = pagination.ProjectedFields.Aggregate((f, s) => $"{f}, {s}");
                var selector = $"new {{ {toProject} }}";
                q = q.Select<TDbRecord>(selector).ToDynamicArray<TDbRecord>().AsQueryable();
            }
            
            var pageData = q.ToArray();
            _logger.LogDebug(EfRepositoryEventIds.Read, $"{nameof(GetAll)} {pageData} = {pageData.ToJsonString()}");
            DetachEntities(q);

            _logger.LogDebug(EfRepositoryEventIds.Read, $"{nameof(GetAll)} total entities in page: " + pageData.Count());
            return Task.FromResult(pageData as IEnumerable<TDbRecord>);
        }
        private static string GetOrderByProperty(string propertyName)
        {
            if (!propertyName.HasValue())
                return nameof(IEntity.Id);
            var pi = GetTypePropertyInfos().FirstOrDefault(x => OrderByPropertyGetFunction(x.Name, propertyName));
            return pi == default ? nameof(IEntity.Id) : pi.Name;
        }
        public virtual async Task<TDbRecord> GetById(TId id)
        {
            _logger.LogDebug(EfRepositoryEventIds.Read, $"{nameof(GetById)} with id = {id}");
            var entity = await GetEntityById_Internal(id);
            if (entity == null)
                return null;
            DetachEntities(new[] { entity });
            _logger.LogDebug(EfRepositoryEventIds.Read, $"{nameof(GetById)} result = {entity.ToJsonString()}");
            return entity;
        }
        public virtual async Task<TDbRecord> Insert(TDbRecord entity)
        {
            _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(Insert)} with entity = {entity.ToJsonString()}");
            await _dbContext.Set<TDbRecord>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            DetachEntities(new[] { entity });
            _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(Insert)} result = {entity.ToJsonString()}");
            return entity;
        }
        public virtual async Task<IEnumerable<TDbRecord>> BulkInsert(IEnumerable<TDbRecord> entities, bool trackIds = false)
        {
            _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(BulkInsert)} with entity = {entities.ToJsonString()}");
            _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(BulkInsert)} bulk operation started");
            var inserted = new List<TDbRecord>();
            try
            {
                int offset = 0,
                curCount = _config.InsertBatchSize;
                var tasks = new List<Task>();
                do
                {
                    var curBatch = entities.Skip(offset).Take(_config.InsertBatchSize);
                    curCount = curBatch.Count();
                    offset += _config.InsertBatchSize;
                    tasks.Add(insertBatchToDatabase(curBatch));
                } while (curCount == _config.InsertBatchSize);
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(BulkInsert)} Exception was thrown: {ex.Message}");
                throw ex;
            }
            _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(BulkInsert)} Bulk operation ended");
            return inserted;

            async Task insertBatchToDatabase(IEnumerable<TDbRecord> bulk)
            {
                using var sc = _serviceProvider.CreateScope();
                using var ctx = sc.ServiceProvider.GetService<DbContext>();
                ctx.ChangeTracker.AutoDetectChangesEnabled = trackIds;
                await ctx.Set<TDbRecord>().AddRangeAsync(bulk);
                await ctx.SaveChangesAsync();
                inserted.AddRange(bulk);
            }
        }
        public virtual async Task<TDbRecord> Update(TDbRecord entity)
        {
            _logger.LogDebug(EfRepositoryEventIds.Update, $"{nameof(Update)} with entity = {entity.ToJsonString()}");
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
            DetachEntities(new[] { dbEntity });
            _logger.LogDebug(EfRepositoryEventIds.Update, $"{nameof(Update)} result = {entity.ToJsonString()}");
            return dbEntity;
        }

        public virtual async Task<IEnumerable<TDbRecord>> BulkUpdate(IEnumerable<TDbRecord> entities, bool trackIds = false)
        {
            _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(BulkUpdate)} with entity = {entities.ToJsonString()}");
            _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(BulkUpdate)} bulk operation started");
            var updated = new List<TDbRecord>();
            try
            {
                int offset = 0,
                curCount = _config.UpdateBatchSize;
                var tasks = new List<Task>();
                do
                {
                    var curBatch = entities.Skip(offset).Take(_config.UpdateBatchSize);
                    curCount = curBatch.Count();
                    offset += _config.UpdateBatchSize;
                    tasks.Add(updatetBatchInDatabase(curBatch));
                } while (curCount == _config.UpdateBatchSize);
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(EfRepositoryEventIds.Update, $"{nameof(BulkUpdate)} Exception was thrown: {ex.Message}");
                return null;
            }
            _logger.LogDebug(EfRepositoryEventIds.Update, $"{nameof(BulkUpdate)} Bulk operation ended");
            return updated;

            async Task updatetBatchInDatabase(IEnumerable<TDbRecord> batch)
            {
                using var sc = _serviceProvider.CreateScope();
                using var ctx = sc.ServiceProvider.GetService<DbContext>();
                var set = ctx.Set<TDbRecord>().AsNoTracking();
                var allIds = batch.Select(s => s.Id);
                var allDbEntries = set.Where(e => allIds.Contains(e.Id));
                if (allDbEntries.IsNullOrEmpty())
                    return;
                DetachEntities(allDbEntries);

                foreach (var b in batch)
                {
                    var dbEntry = allDbEntries.FirstOrDefault(x => x.Id.Equals(b.Id));
                    if (dbEntry == null) continue;

                    foreach (var pInfo in GetTypePropertyInfos())
                    {
                        if (pInfo.Name == nameof(IEntity.Id)) continue;
                        var value = pInfo.GetValue(b);
                        pInfo.SetValue(dbEntry, value);
                    }
                    _dbContext.Update(dbEntry);
                    updated.Add(dbEntry);
                }

                await _dbContext.SaveChangesAsync();
                ctx.Set<TDbRecord>().UpdateRange(batch);
                await ctx.SaveChangesAsync();
            }
        }
        public virtual async Task<TDbRecord> Delete(TDbRecord entity)
        {
            _logger.LogDebug(EfRepositoryEventIds.Delete, $"{nameof(Delete)} with entity = {entity.ToJsonString()}");
            var dbEntity = await GetEntityById_Internal(entity.Id);
            if (dbEntity == null)
                return null;
            var entry = _dbContext.Remove(dbEntity);
            await _dbContext.SaveChangesAsync();
            entry.State = EntityState.Detached;
            _logger.LogDebug(EfRepositoryEventIds.Delete, $"{nameof(Delete)} result = {entity.ToJsonString()}");
            return entity;
        }
        private async Task<TDbRecord> GetEntityById_Internal(TId id)
        {
            var query = _collection.Where(x => x.Id.Equals(id));
            return await IncludeNavigations(query).FirstOrDefaultAsync();
        }
        private void DetachEntities(IEnumerable<TDbRecord> entities)
        {
            _logger.LogDebug(EfRepositoryEventIds.Read, $"{nameof(DetachEntities)} Detaching entities");
            foreach (var e in entities)
            {
                _dbContext.Entry(e).State = EntityState.Detached;
                foreach (var col in _dbContext.Entry(e).Collections)
                    col.EntityEntry.State = EntityState.Detached;
            }
        }
        private static readonly ConcurrentDictionary<Type, IEnumerable<string>> NavigationPropertyNames
            = new ConcurrentDictionary<Type, IEnumerable<string>>();
        private IQueryable<TDbRecord> IncludeNavigations(IEnumerable<TDbRecord> source)
        {
            var type = typeof(TDbRecord);

            if (!NavigationPropertyNames.TryGetValue(type, out IEnumerable<string> navigationPropertiesNames))
            {
                var allProperties = _dbContext.Model.FindEntityType(typeof(TDbRecord));
                navigationPropertiesNames = allProperties.GetNavigations().Select(x => x.Name).ToArray();
                NavigationPropertyNames.TryAdd(type, navigationPropertiesNames);
            }
            foreach (var name in navigationPropertiesNames)
                source = source.AsQueryable().Include(name);
            return source.AsQueryable();
        }
        private static IEnumerable<PropertyInfo> GetTypePropertyInfos()
        {
            var type = typeof(TDbRecord);
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