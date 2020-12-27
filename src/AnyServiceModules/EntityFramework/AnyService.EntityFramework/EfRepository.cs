using AnyService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace AnyService.EntityFramework
{
    public class EfRepository<TDbModel> : EfGenericRepository<TDbModel, string>,
        IRepository<TDbModel>
        where TDbModel : class, IDbModel<string>
    {
        public EfRepository(
            DbContext dbContext,
            EfRepositoryConfig config,
            IServiceProvider serviceProvider,
            ILogger<EfGenericRepository<TDbModel, string>> logger)
            : base(dbContext, config, serviceProvider, logger)
        {
        }
    }
}