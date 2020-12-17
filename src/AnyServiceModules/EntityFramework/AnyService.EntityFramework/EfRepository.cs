using AnyService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AnyService.EntityFramework
{
    public class EfRepository<TDbModel> : EfGenericRepository<TDbModel, string>,
        IRepository<TDbModel>
        where TDbModel : class, IDbModel<string>
    {
        public EfRepository(DbContext dbContext, EfRepositoryConfig config, ILogger<EfGenericRepository<TDbModel, string>> logger) : base(dbContext, config, logger)
        {
        }
    }
}