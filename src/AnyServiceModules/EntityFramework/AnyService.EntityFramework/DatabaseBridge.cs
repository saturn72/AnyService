using AnyService.Services;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AnyService.EntityFramework
{
    internal class DatabaseBridge<TDbModel> where TDbModel : class
    {
        #region Fields
        private static string _selectByIdSqlCommandFormat;
        private static string _pkColumnName;
        private static readonly ConcurrentDictionary<Type, IEnumerable<string>> NavigationPropertyNames = new ConcurrentDictionary<Type, IEnumerable<string>>();
        private static readonly ConcurrentDictionary<Type, IEnumerable<PropertyInfo>> TypeProperties = new ConcurrentDictionary<Type, IEnumerable<PropertyInfo>>();

        private readonly DbContext _dbContext;
        private readonly ILogger _logger;
        #endregion
        #region ctor
        public DatabaseBridge(DbContext dbContext, ILogger logger)
        {
            _dbContext = dbContext;
            _logger = logger;

            if (_selectByIdSqlCommandFormat == null)
            {
                _pkColumnName = _dbContext.Model.FindEntityType(typeof(TDbModel)).FindPrimaryKey().Properties.First().GetColumnName();
                var tableName = _dbContext.Model.FindEntityType(typeof(TDbModel)).GetTableName();
                _selectByIdSqlCommandFormat = $"SELECT * FROM {tableName} WHERE {_pkColumnName} = '{{0}}'";
            }
        }
        #endregion
        internal IQueryable<TDbModel> Collection
        {
            get
            {
                _logger.LogDebug(EfRepositoryEventIds.EfRepositoryBridge, "Get database collection");
                return _dbContext.Set<TDbModel>().AsNoTracking();
            }
        }
        #region BulkInsert
        internal async Task<IEnumerable<TDbModel>> BulkInsert(IEnumerable<TDbModel> entities, bool trackIds = false)
        {
            _logger.LogDebug(EfRepositoryEventIds.EfRepositoryBridge, $"{nameof(BulkInsert)} with entity = {entities.ToJsonString()}");
            if (trackIds)
            {
                await _dbContext.Set<TDbModel>().AddRangeAsync(entities.ToArray());
                _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(BulkInsert)} bulk operation started");
                await _dbContext.SaveChangesAsync();
                _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(BulkInsert)} Bulk operation ended");
                _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(BulkInsert)} result = {entities.ToJsonString()}");
                await DetachEntities(entities);
                return entities;
            }

            _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(BulkInsert)} bulk operation started");
            await _dbContext.BulkInsertAsync(entities.ToList());
            _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(BulkInsert)} Bulk operation ended");
            return entities;
        }
        internal async Task<TDbModel> Insert(TDbModel entity)
        {
            _logger.LogDebug(EfRepositoryEventIds.EfRepositoryBridge, $"{nameof(Insert)} with entity = {entity.ToJsonString()}");
            _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(Insert)} with entity = {entity.ToJsonString()}");
            await _dbContext.Set<TDbModel>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            await DetachEntities(new[] { entity });
            _logger.LogDebug(EfRepositoryEventIds.Create, $"{nameof(Insert)} result = {entity.ToJsonString()}");
            return entity;
        }
        #endregion
        #region read
        internal async Task<IEnumerable<TDbModel>> GetAll(Pagination<TDbModel> pagination)
        {
            if (pagination == null || pagination.QueryFunc == null)
                throw new ArgumentNullException(nameof(pagination));
            _logger.LogDebug(EfRepositoryEventIds.Read, "Get all with pagination: " + pagination.QueryOrFilter ?? pagination.QueryFunc.ToString());

            _logger.LogDebug(EfRepositoryEventIds.Read, "Get all with pagination: " + pagination.QueryOrFilter ?? pagination.QueryFunc.ToString());
            pagination.Total = Collection.Where(pagination.QueryFunc).Count();
            _logger.LogDebug(EfRepositoryEventIds.Read, "GetAll set total to: " + pagination.Total);

            var q = pagination.IncludeNested ? IncludeNavigations(Collection) : Collection;

            var isDesc = pagination.SortOrder == PaginationSettings.Desc;
            var orderByPropertyName = GetOrderByProperty(pagination.OrderBy);
            q = q.OrderBy(orderByPropertyName, isDesc)
                    .Where(pagination.QueryFunc).AsQueryable();

            q = pagination.Offset == 0 ?
                   q.Take(pagination.PageSize) :
                   q.Skip(pagination.Offset).Take(pagination.PageSize);

            var page = q.ToArray();
            _logger.LogDebug(EfRepositoryEventIds.Read, "GetAll Detaching entities");
            await DetachEntities(q);

            _logger.LogDebug(EfRepositoryEventIds.Read, "GetAll total entities in page: " + page.Count());
            return page;
        }
        internal async Task<TDbModel> GetById(string id)
        {
            _logger.LogDebug(EfRepositoryEventIds.Read, $"{nameof(GetById)} with id = {id}");
            var entity = GetEntityById_Internal(id);
            if (entity == null)
                return null;
            await DetachEntities(new[] { entity });
            _logger.LogDebug(EfRepositoryEventIds.Read, $"{nameof(GetById)} result = {entity.ToJsonString()}");
            return entity;
        }
        #endregion
        #region Update
        internal async Task<TDbModel> Update(TDbModel entity)
        {
            _logger.LogDebug(EfRepositoryEventIds.EfRepositoryBridge, $"{nameof(Update)} with entity = {entity.ToJsonString()}");
            var id = entity.GetPropertyValueByName<object>(_pkColumnName);
            var dbEntity = GetEntityById_Internal(id);

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
        #endregion 
        #region Delete
        internal async Task<TDbModel> Delete(TDbModel entity)
        {
            _logger.LogDebug(EfRepositoryEventIds.EfRepositoryBridge, $"{nameof(Delete)} with entity = {entity.ToJsonString()}");
            var id = entity.GetPropertyValueByName<object>(_pkColumnName);

            var dbEntity = GetEntityById_Internal(id);
            if (dbEntity == null)
                return null;
            var entry = _dbContext.Remove(dbEntity);
            await _dbContext.SaveChangesAsync();
            entry.State = EntityState.Detached;
            _logger.LogDebug(EfRepositoryEventIds.Delete, $"{nameof(Delete)} result = {entity.ToJsonString()}");
            return entity;
        }
        #endregion

        #region Utilities
        private TDbModel GetEntityById_Internal(object id)
        {
            var sql = string.Format(_selectByIdSqlCommandFormat, id);
            var query = _dbContext.Set<TDbModel>().FromSqlRaw(sql).AsNoTracking().ToArray();
            return IncludeNavigations(query).FirstOrDefault();
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
        private static string GetOrderByProperty(string paginationOrderBy)
        {
            return paginationOrderBy != null && GetTypePropertyInfos().Any(x => x.Name == paginationOrderBy) ?
                paginationOrderBy : nameof(IDomainEntity.Id);
        }
        #endregion
    }
}
