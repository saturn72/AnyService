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
    public class EfGenericRepository<TDbModel, TId> : IGenericRepository<TDbModel, TId>
    where TDbModel : class, IDbModel<TId>

    {
        #region fields
        private static readonly ConcurrentDictionary<Type, IEnumerable<PropertyInfo>> TypeProperties = new ConcurrentDictionary<Type, IEnumerable<PropertyInfo>>();
        private static Func<string, string, bool> OrderByPropertyGetFunction;

        private readonly DbContext _dbContext;
        private readonly EfRepositoryConfig _config;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EfGenericRepository<TDbModel, TId>> _logger;
        private readonly IQueryable<TDbModel> _collection;
        #endregion
        #region ctor
        public EfGenericRepository(
            DbContext dbContext,
            EfRepositoryConfig config,
            IServiceProvider serviceProvider,
            ILogger<EfGenericRepository<TDbModel, TId>> logger)
        {
            _dbContext = dbContext;
            _config = config;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _collection = _dbContext.Set<TDbModel>().AsNoTracking();
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
        public Task<IQueryable<TDbModel>> Collection => Task.FromResult(_collection);
        public virtual async Task<IEnumerable<TDbModel>> GetAll(Pagination<TDbModel> pagination)
        {
            if (pagination == null || pagination.QueryFunc == null)
                throw new ArgumentNullException(nameof(pagination));
            _logger.LogDebug(EfRepositoryEventIds.Read, "Get all with pagination: " + pagination.QueryOrFilter ?? pagination.QueryFunc.ToString());
            pagination.Total = _collection.Where(pagination.QueryFunc).Count();
            _logger.LogDebug(EfRepositoryEventIds.Read, "GetAll set total to: " + pagination.Total);

            var q = pagination.IncludeNested ? IncludeNavigations(_collection) : _collection;
            var isDesc = pagination.SortOrder == PaginationSettings.Desc;
            var orderByPropertyName = GetOrderByProperty(pagination.OrderBy);
            q = q.OrderBy(orderByPropertyName, isDesc)
                .Where(pagination.QueryFunc).AsQueryable();

            q = pagination.Offset == 0 ?
               q.Take(pagination.PageSize) :
               q.Skip(pagination.Offset).Take(pagination.PageSize);

            if (!pagination.ProjectedFields.IsNullOrEmpty())
            {
                var toProject = pagination.ProjectedFields.Aggregate((f, s) => $"{f}, {s}");
                var selector = $"new {{ {toProject} }}";
                q = q.Select<TDbModel>(selector).ToDynamicArray<TDbModel>().AsQueryable();
            }

            var page = q.ToArray();
            _logger.LogDebug(EfRepositoryEventIds.Read, "GetAll Detaching entities");
            await DetachEntities(q);

            _logger.LogDebug(EfRepositoryEventIds.Read, "GetAll total entities in page: " + page.Count());
            return page;
        }
        private static string GetOrderByProperty(string propertyName)
        {
            if (!propertyName.HasValue())
                return nameof(IEntity.Id);
            var pi = GetTypePropertyInfos().FirstOrDefault(x => OrderByPropertyGetFunction(x.Name, propertyName));
            return pi == default ? nameof(IEntity.Id) : pi.Name;
        }

        public virtual async Task<TDbModel> GetById(TId id)
        {
            _logger.LogDebug(EfRepositoryEventIds.Read, $"{nameof(GetById)} with id = {id}");
            var entity = await GetEntityById_Internal(id);
            if (entity == null)
                return null;
            await DetachEntities(new[] { entity });
            _logger.LogDebug(EfRepositoryEventIds.Read, $"{nameof(GetById)} result = {entity.ToJsonString()}");
            return entity;
        }
        public virtual async Task<TDbModel> Insert(TDbModel entity)
        {
            _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(Insert)} with entity = {entity.ToJsonString()}");
            await _dbContext.Set<TDbModel>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            await DetachEntities(new[] { entity });
            _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(Insert)} result = {entity.ToJsonString()}");
            return entity;
        }
        public virtual async Task<IEnumerable<TDbModel>> BulkInsert(IEnumerable<TDbModel> entities, bool trackIds = false)
        {
            _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(BulkInsert)} with entity = {entities.ToJsonString()}");
            _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(BulkInsert)} bulk operation started");
            var inserted = new List<TDbModel>();
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
                _logger.LogError(EfRepositoryEventIds.Create, $"{nameof(BulkInsert)} Exception was thrown: {ex.Message}");
                return null;
            }
            _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(BulkInsert)} Bulk operation ended");
            return inserted;

            async Task insertBatchToDatabase(IEnumerable<TDbModel> bulk)
            {
                using var sc = _serviceProvider.CreateScope();
                using var ctx = sc.ServiceProvider.GetService<DbContext>();
                ctx.ChangeTracker.AutoDetectChangesEnabled = trackIds;
                await ctx.Set<TDbModel>().AddRangeAsync(bulk);
                await ctx.SaveChangesAsync();
                inserted.AddRange(bulk);
            }
        }
        //protected virtual async Task<IEnumerable<TDbModel>> BulkInsertAndTrack(IEnumerable<TDbModel> entities)
        //{
        //    _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(BulkInsert)} bulk operation started");
        //    var set = _dbContext.Set<TDbModel>();
        //    await set.AddRangeAsync(entities.ToArray());
        //    await _dbContext.SaveChangesAsync();
        //    _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(BulkInsert)} Bulk operation ended");
        //    _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(BulkInsert)} result = {entities.ToJsonString()}");
        //    await DetachEntities(entities);
        //    return entities;
        //}

        public virtual async Task<TDbModel> Update(TDbModel entity)
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
            await DetachEntities(new[] { dbEntity });
            _logger.LogDebug(EfRepositoryEventIds.Update, $"{nameof(Update)} result = {entity.ToJsonString()}");
            return dbEntity;
        }
        public virtual async Task<TDbModel> Delete(TDbModel entity)
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
        private async Task<TDbModel> GetEntityById_Internal(TId id)
        {
            var query = _collection.Where(x => x.Id.Equals(id));
            return await IncludeNavigations(query).FirstOrDefaultAsync();
        }
        private Task DetachEntities(IEnumerable<TDbModel> entities)
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
        private IQueryable<TDbModel> IncludeNavigations(IEnumerable<TDbModel> source)
        {
            var type = typeof(TDbModel);

            if (!NavigationPropertyNames.TryGetValue(type, out IEnumerable<string> navigationPropertiesNames))
            {
                var allProperties = _dbContext.Model.FindEntityType(typeof(TDbModel));
                navigationPropertiesNames = allProperties.GetNavigations().Select(x => x.Name).ToArray();
                NavigationPropertyNames.TryAdd(type, navigationPropertiesNames);
            }
            foreach (var name in navigationPropertiesNames)
                source = source.AsQueryable().Include(name);
            return source.AsQueryable();
        }
        private static IEnumerable<PropertyInfo> GetTypePropertyInfos()
        {
            var type = typeof(TDbModel);
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