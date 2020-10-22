using AnyService.Services;
using LinqToDB.Configuration;
using Microsoft.Extensions.Logging;

namespace AnyService.LinqToDb
{
    public class LinqToDbRepository<TEntity> : LinqToDbGenericRepository<TEntity, string>, IRepository<TEntity> where TEntity : class, IEntity
    {
        public LinqToDbRepository(LinqToDbConnectionOptions connectionOptions, ILogger<LinqToDbGenericRepository<TEntity, string>> logger) : base(connectionOptions, logger)
        {
        }
    }
}
