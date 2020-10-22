using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public static class RepositoryExtensions
    {
        public static async Task<IQueryable<TDomainEntity>> GetBy<TDomainEntity>(this IRepository<TDomainEntity> repo, Expression<Func<TDomainEntity, bool>> predicate)
           where TDomainEntity : IEntity
        {
            var col = await repo.Collection;
            return col.Where(predicate);
        }
    }
}
