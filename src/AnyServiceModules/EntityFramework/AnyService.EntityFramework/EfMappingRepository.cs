using AnyService.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using Microsoft.Extensions.Logging;
using SQLitePCL;
using AnyService.Mapping;

namespace AnyService.EntityFramework
{
    public class EfMappingRepository<TDomainModel, TDbModel> : IRepository<TDomainModel>
        where TDomainModel : class, IDomainEntity
        where TDbModel : class
    {
        private readonly string _mapperName;
        private readonly DbContext _dbContext;
        private readonly IQueryable<TDbModel> _collection;
        private readonly ILogger<EfMappingRepository<TDomainModel, TDbModel>> _logger;

        public EfMappingRepository(
            string mapperName,
            DbContext dbContext,
            ILogger<EfMappingRepository<TDomainModel, TDbModel>> logger)
        {
            _mapperName = mapperName;
            _dbContext = dbContext;
            _logger = logger;
            _collection = _dbContext.Set<TDbModel>().AsNoTracking();

        }
        public Task<IQueryable<TDomainModel>> Collection
        {
            get
            {
                var mappedCol = _collection.Map<IEnumerable<TDomainModel>>(_mapperName);
                return Task.FromResult(mappedCol.AsQueryable());
            }
        }

        public Task<IEnumerable<TDomainModel>> BulkInsert(IEnumerable<TDomainModel> entities, bool track = false)
        {
            throw new NotImplementedException();
        }

        public Task<TDomainModel> Delete(TDomainModel entity)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<TDomainModel>> GetAll(Pagination<TDomainModel> paginate)
        {
            throw new NotImplementedException();
        }

        public Task<TDomainModel> GetById(string id)
        {
            throw new NotImplementedException();
        }

        public Task<TDomainModel> Insert(TDomainModel entity)
        {
            throw new NotImplementedException();
        }

        public Task<TDomainModel> Update(TDomainModel entity)
        {
            throw new NotImplementedException();
        }
    }
}