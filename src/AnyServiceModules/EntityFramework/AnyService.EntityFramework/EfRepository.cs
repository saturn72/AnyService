using AnyService.Services;
using AnyService.Core;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore.ValueGeneration;

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

        public async Task<IEnumerable<TDomainModel>> GetAll(IDictionary<string, string> filter = null)
        {
            if (filter == null)
                return await Entities.ToArrayAsync();

            var query = ExpressionBuilder.ToExpression<TDomainModel>(filter);
            if (query == null)
                return null;
            return await Entities.Where(query).ToArrayAsync();
        }

        public Task<TDomainModel> GetById(string id) =>
            Entities.FirstOrDefaultAsync(x => x.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase));

        public async Task<TDomainModel> Insert(TDomainModel entity)
        {
            await _dbContext.Set<TDomainModel>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            _dbContext.Entry(entity).State = EntityState.Detached;
            return entity;
        }

        public async Task<TDomainModel> Update(TDomainModel entity)
        {
            _dbContext.Set<TDomainModel>().Update(entity);
            await _dbContext.SaveChangesAsync();
            _dbContext.Entry(entity).State = EntityState.Detached;
            return entity;
        }
    }
}