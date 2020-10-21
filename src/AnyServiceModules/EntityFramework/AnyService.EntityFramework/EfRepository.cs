using AnyService.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using Microsoft.Extensions.Logging;
using EFCore.BulkExtensions;

namespace AnyService.EntityFramework
{
    public class EfRepository<TDomainModel> : IRepository<TDomainModel>
        where TDomainModel : class, IDomainEntity
    {
        #region fields

        private readonly DbContext _dbContext;
        private readonly ILogger<EfRepository<TDomainModel>> _logger;
        private readonly DatabaseBridge<TDomainModel> _bridge;
        private static readonly BulkConfig InsertBulkConfig = new BulkConfig
        {
            PreserveInsertOrder = true,
            PropertiesToExclude = new List<string> { nameof(IDomainEntity.Id) },
        };
        #endregion
        #region ctor
        public EfRepository(DbContext dbContext, ILogger<EfRepository<TDomainModel>> logger)
        {
            _dbContext = dbContext;
            _logger = logger;

            _bridge = new DatabaseBridge<TDomainModel>(dbContext, logger);
        }
        #endregion

        public Task<IQueryable<TDomainModel>> Collection => Task.FromResult(_bridge.Collection);
        #region Create
        public virtual async Task<IEnumerable<TDomainModel>> BulkInsert(IEnumerable<TDomainModel> entities, bool trackIds = false)
        {
            _logger.LogInformation(EfRepositoryEventIds.Create, $"{nameof(BulkInsert)} with entity = {entities.ToJsonString()}");
            return await _bridge.BulkInsert(entities, trackIds);
        }
        public virtual async Task<TDomainModel> Insert(TDomainModel entity)
        {
            _logger.LogInformation(EfRepositoryEventIds.Create, $"{nameof(Insert)} with entity = {entity.ToJsonString()}");
            return await _bridge.Insert(entity);
        }
        #endregion
        public virtual async Task<IEnumerable<TDomainModel>> GetAll(Pagination<TDomainModel> pagination)
        {
            _logger.LogInformation(EfRepositoryEventIds.Read, "Get all with pagination filter: " + pagination?.QueryOrFilter ?? pagination?.QueryFunc?.ToString());
            return await _bridge.GetAll(pagination);
        }
        public virtual async Task<TDomainModel> GetById(string id)
        {
            _logger.LogInformation(EfRepositoryEventIds.Read, $"{nameof(GetById)} with id = {id}");
            return await _bridge.GetById(id);
        }
        public virtual async Task<TDomainModel> Update(TDomainModel entity)
        {
            _logger.LogInformation(EfRepositoryEventIds.Update, $"{nameof(Update)} with entity = {entity.ToJsonString()}");
            return await _bridge.Update(entity);
        }
        public virtual async Task<TDomainModel> Delete(TDomainModel entity)
        {
            _logger.LogInformation(EfRepositoryEventIds.Delete, $"{nameof(Delete)} with entity = {entity.ToJsonString()}");
            return await _bridge.Delete(entity);
        }
    }
}