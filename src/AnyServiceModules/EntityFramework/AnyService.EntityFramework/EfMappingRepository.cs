using AnyService.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using Microsoft.Extensions.Logging;

namespace AnyService.EntityFramework
{
    public class EfMappingRepository<TDomainEntity, TDbModel> : IRepository<TDomainEntity>
        where TDomainEntity : class, IDomainEntity
        where TDbModel : class
    {
        private readonly string _mapperName;
        private readonly DatabaseBridge<TDbModel> _bridge;
        private readonly ILogger<EfMappingRepository<TDomainEntity, TDbModel>> _logger;

        public EfMappingRepository(
            string mapperName,
            DbContext dbContext,
            ILogger<EfMappingRepository<TDomainEntity, TDbModel>> logger)
        {
            _mapperName = mapperName;
            _logger = logger;
            _bridge = new DatabaseBridge<TDbModel>(dbContext, logger);

        }
        public Task<IQueryable<TDomainEntity>> Collection
        {
            get
            {
                var mappedCol = _bridge.Collection.Map<IEnumerable<TDomainEntity>>(_mapperName);
                return Task.FromResult(mappedCol.AsQueryable());
            }
        }
        public async Task<IEnumerable<TDomainEntity>> BulkInsert(IEnumerable<TDomainEntity> entities, bool track = false)
        {
            _logger.LogInformation(EfRepositoryEventIds.Create, $"{nameof(BulkInsert)} with entity = {entities.ToJsonString()}");
            var toInsert = entities.Map<IEnumerable<TDbModel>>(_mapperName);

            _logger.LogDebug(EfRepositoryEventIds.Create, $"Entities mapped to {typeof(TDbModel).Name} = {toInsert.ToJsonString()}");
            var entries = await _bridge.BulkInsert(toInsert, track);
            var res = entries.Map<IEnumerable<TDomainEntity>>(_mapperName);
            _logger.LogDebug(EfRepositoryEventIds.Create, $"Db Records mapped to {typeof(TDomainEntity).Name} = {res.ToJsonString()}");

            return res;
        }
        public async Task<TDomainEntity> Insert(TDomainEntity entity)
        {
            _logger.LogInformation(EfRepositoryEventIds.Create, $"{nameof(Insert)} with entity = {entity?.ToJsonString()}");
            return await ExecuteAndMap(entity, e => _bridge.Insert(e), EfRepositoryEventIds.Create);
        }
        public async Task<IEnumerable<TDomainEntity>> GetAll(Pagination<TDomainEntity> paginate)
        {
            _logger.LogInformation(EfRepositoryEventIds.Read, "Get all with pagination: " + paginate.QueryOrFilter ?? paginate.QueryFunc.ToString());
            var p = paginate.Map<Pagination<TDbModel>>();
            var res = await _bridge.GetAll(p);
            return res.Map<IEnumerable<TDomainEntity>>();
        }
        public virtual async Task<TDomainEntity> GetById(string id)
        {
            _logger.LogInformation(EfRepositoryEventIds.Read, $"{nameof(GetById)} with id = {id}");
            var entry = await _bridge.GetById(id);
            var res = entry.Map<TDomainEntity>(_mapperName);
            _logger.LogDebug(EfRepositoryEventIds.Read, $"Db Record mapped to {typeof(TDomainEntity).Name} = {res.ToJsonString()}");
            return res;
        }
        public virtual async Task<TDomainEntity> Update(TDomainEntity entity)
        {
            _logger.LogInformation(EfRepositoryEventIds.Update, $"{nameof(Update)} with entity = {entity.ToJsonString()}");
            return await ExecuteAndMap(entity, e => _bridge.Update(e), EfRepositoryEventIds.Update);
        }
        public virtual async Task<TDomainEntity> Delete(TDomainEntity entity)
        {
            _logger.LogInformation(EfRepositoryEventIds.Delete, $"{nameof(Delete)} with entity = {entity.ToJsonString()}");
            return await ExecuteAndMap(entity, e => _bridge.Delete(e), EfRepositoryEventIds.Delete);
        }
        private async Task<TDomainEntity> ExecuteAndMap(
            TDomainEntity entity,
            Func<TDbModel, Task<TDbModel>> func,
            EventId eventId)
        {
            var e = entity.Map<TDbModel>(_mapperName);
            _logger.LogDebug(eventId, $"Entity mapped to {typeof(TDbModel).Name} = {e.ToJsonString()}");
            var entry = await func(e);
            _logger.LogDebug(eventId, $"Db returned {typeof(TDbModel).Name} = {entry?.ToJsonString()}");
            if (entry == null)
                return default;
            var res = entry.Map<TDomainEntity>(_mapperName);
            _logger.LogDebug(eventId, $"Db Record mapped to {typeof(TDomainEntity).Name} = {res.ToJsonString()}");
            return res;
        }
    }
}