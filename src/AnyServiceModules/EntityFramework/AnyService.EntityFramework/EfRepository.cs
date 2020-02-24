using AnyService.Services;
using AnyService.Core;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using System.Collections.Concurrent;

namespace AnyService.EntityFramework
{
    public class EfRepository<TDomainModel> : IRepository<TDomainModel>
        where TDomainModel : class, IDomainModelBase
    {
        private readonly DbContext _dbContext;
        private IQueryable<TDomainModel> DbSet => _dbContext.Set<TDomainModel>().AsNoTracking();

        public EfRepository(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<TDomainModel>> GetAll(IDictionary<string, string> filter = null)
        {
            if (filter == null)
                return await IncludeNavigations(DbSet).ToArrayAsync();

            var query = ExpressionBuilder.ToExpression<TDomainModel>(filter);
            if (query == null)
                return null;
            return await IncludeNavigations(DbSet.Where(query)).ToArrayAsync();
        }
        public Task<TDomainModel> GetById(string id)
        {
            var query = DbSet.Where(x => x.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase));
            return IncludeNavigations(query).FirstOrDefaultAsync();
        }

        public async Task<TDomainModel> Insert(TDomainModel entity)
        {
            await _dbContext.Set<TDomainModel>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            DetachEntity(entity);
            return entity;
        }
        public async Task<TDomainModel> Update(TDomainModel entity)
        {
            _dbContext.Set<TDomainModel>().Update(entity);
            await _dbContext.SaveChangesAsync();
            DetachEntity(entity);
            return entity;
        }
        private void DetachEntity(TDomainModel entity)
        {
            _dbContext.Entry(entity).State = EntityState.Detached;

            foreach (var col in _dbContext.Entry(entity).Collections)
                col.EntityEntry.State = EntityState.Detached;
        }
        private static readonly ConcurrentDictionary<Type, IEnumerable<string>> NavigationPropertyNames
            = new ConcurrentDictionary<Type, IEnumerable<string>>();
        private IQueryable<TDomainModel> IncludeNavigations(IQueryable<TDomainModel> query)
        {
            var type = typeof(TDomainModel);

            if (!NavigationPropertyNames.TryGetValue(type, out IEnumerable<string> navigationPropertiesNames))
            {
                var allProperties = _dbContext.Model.FindEntityType(typeof(TDomainModel));
                navigationPropertiesNames = allProperties.GetNavigations().Select(x => x.Name).ToArray();
                NavigationPropertyNames.TryAdd(type, navigationPropertiesNames);
            }
            foreach (var name in navigationPropertiesNames)
                query = query.Include(name);
            return query;
        }
    }
}