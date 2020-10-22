using AnyService.Services;
using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.Tools;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace AnyService.LinqToDb
{
    public class LinqToDbGenericRepository<TEntity, TId> : IGenericRepository<TEntity, TId> where TEntity : class, IDbModel<TId>
    {
        #region fields
        private ITable<TEntity> _entities;
        private readonly LinqToDbConnectionOptions _connectionOptions;
        private readonly ILogger<LinqToDbGenericRepository<TEntity, TId>> _logger;
        #endregion

        #region ctor
        public LinqToDbGenericRepository(
            LinqToDbConnectionOptions connectionOptions,
            ILogger<LinqToDbGenericRepository<TEntity, TId>> logger)
        {
            _connectionOptions = connectionOptions;
            _logger = logger;
        }
        #endregion
        protected ITable<TEntity> Entities => _entities ?? ExecuteTransaction(dc => dc.GetTable<TEntity>());
        public Task<IQueryable<TEntity>> Collection => Task.FromResult<IQueryable<TEntity>>(Entities);

        public async Task<IEnumerable<TEntity>> BulkInsert(IEnumerable<TEntity> entities, bool track = false)
        {
            _logger.LogInformation(LinqToDbLoggingEvents.BulkInsert, $"{nameof(BulkInsert)} with data: entities = {entities.ToJsonString()}, track = {track}");

            Func<DataConnection, Task<IEnumerable<TEntity>>> command = async dc =>
            {
                var toInsert = track ? entities.RetrieveIdentity(dc) : entities;
                await dc.BulkCopyAsync(new BulkCopyOptions(), toInsert);
                return toInsert;
            };
            return await ExecuteTransactionAsync(command);
        }
        public async Task<TEntity> Delete(TEntity entity)
        {
            _logger.LogInformation(LinqToDbLoggingEvents.Delete, $"{nameof(Delete)} with data: entity = {entity.ToJsonString()}");

            Func<DataConnection, Task<TEntity>> command = async dc =>
            {
                await dc.DeleteAsync(entity);
                return entity;
            };
            return await ExecuteTransactionAsync(command);
        }

        public Task<IEnumerable<TEntity>> GetAll(Pagination<TEntity> paginate)
        {
            throw new NotImplementedException();
        }
        public async Task<TEntity> GetById(TId id)
        {
            return await Entities.FirstOrDefaultAsync(x => x.Id.Equals(id));
        }
        private T ExecuteTransaction<T>(Func<DataConnection, T> command)
        {
            using var dataConnection = new DataConnection(_connectionOptions);
            using var transaction = new TransactionScope();
            var res = command(dataConnection);
            transaction.Complete();
            return res;
        }
        private async Task<T> ExecuteTransactionAsync<T>(Func<DataConnection, Task<T>> command)
        {
            using var dataConnection = new DataConnection(_connectionOptions);
            using var transaction = new TransactionScope();
            var res = await command(dataConnection);
            transaction.Complete();
            return res;
        }
        private T Execute<T>(Func<DataConnection, T> command)
        {
            using var dataConnection = new DataConnection(_connectionOptions);
            return command(dataConnection);
        }
        private async Task<T> ExecuteAsync<T>(Func<DataConnection, Task<T>> command)
        {
            using var dataConnection = new DataConnection(_connectionOptions);
            return await command(dataConnection);
        }


        public Task<TEntity> Insert(TEntity entity)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> Update(TEntity entity)
        {
            throw new NotImplementedException();
        }
    }
}
