using AnyService.Services;
using AnyService.Core;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;

namespace AnyService.EntityFramework
{
    public class EfRepository<TDomainModel> : IRepository<TDomainModel>
    where TDomainModel : class, IDomainModelBase
    {
        private readonly DbContext _dbContext;
        private IQueryable<TDomainModel> Entities => _entities ?? (_entities = _dbContext.Set<TDomainModel>().AsNoTracking());
        private IQueryable<TDomainModel> _entities;

        public EfRepository(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<IEnumerable<TDomainModel>> GetAll(IDictionary<string, string> filter = null) =>
            Task.FromResult<IEnumerable<TDomainModel>>(Entities.ToArray());

        public Task<TDomainModel> GetById(string id) =>
            Entities.FirstOrDefaultAsync(x => x.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase));

        public async Task<TDomainModel> Insert(TDomainModel entity)
        {
            await _dbContext.Set<TDomainModel>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task<TDomainModel> Update(TDomainModel entity)
        {
            _dbContext.Set<TDomainModel>().Update(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }
    }
}