using AnyService.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Services.Logging
{
    public class LogRecordManager : ILogManager
    {
        #region Fields
        private readonly IRepository<LogRecord> _repository;
        private readonly ILogger<LogRecordManager> _logger;
        #endregion

        #region ctor
        public LogRecordManager(IRepository<LogRecord> repository, ILogger<LogRecordManager> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        #endregion
        public Task<LogRecord> InsertLogRecord(LogRecord logRecord)
        {
            _logger.LogInformation(LoggingEvents.BusinessLogicFlow, $"insert log record to {nameof(IRepository<LogRecord>)}");
            return _repository.Insert(logRecord);
        }
        public Task<LogRecordPagination> GetAll(LogRecordPagination pagination)
        {

            _logger.LogInformation(LoggingEvents.BusinessLogicFlow, "Start get all log records flow");
            pagination.QueryFunc = BuildLogRecordPaginationQuery(pagination);
            throw new NotImplementedException();

            //_logger.LogDebug(LoggingEvents.Repository, "Get all audit-records from repository using paginate = " + pagination);

            //var serviceResponse = new ServiceResponse { Data = pagination };
            //var wrapper = new ServiceResponseWrapper(serviceResponse);
            //var data = await _repository.Query(r => r.GetAll(pagination), wrapper);
            //_logger.LogDebug(LoggingEvents.Repository, $"Repository response: {data.ToJsonString()}");
            //if (serviceResponse.Result != ServiceResult.NotSet)
            //    return serviceResponse;

            //pagination.Data = data ?? new AuditRecord[] { };
            //serviceResponse.Data = pagination;
            //serviceResponse.Result = ServiceResult.Ok;
            //_logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Service Response: {serviceResponse}");
            //return serviceResponse;
        }
        protected Func<LogRecord, bool> BuildLogRecordPaginationQuery(LogRecordPagination pagination)
        {
            var logRecordIdQuery = getCollectionQuery(pagination.LogRecordIds, a => a.Id);
            var logLevelQuery = getCollectionQuery(pagination.LogLevels, a => a.Level);
            var userIdQuery = getCollectionQuery(pagination.UserIds, a => a.UserId);
            var clientIdQuery = getCollectionQuery(pagination.ClientIds, a => a.ClientId);
            var exceptionIdQuery = getCollectionQuery(pagination.ExceptionIds, a => a.ExceptionId);
            var exceptionRuntimeTypeQuery = getCollectionQuery(pagination.ExceptionRuntimeTypes, a => a.ExceptionRuntimeType);
            var ipAddressQuery = getCollectionQuery(pagination.IpAddresses, a => a.IpAddress);
            var httpMethodQuery = getCollectionQuery(pagination.HttpMethods, a => a.HttpMethod);
            var exceptionRuntimeMessageQuery = getCollectionOrContainuationQuery(pagination.ExceptionRuntimeMessageContains, pagination.ExceptionRuntimeMessages, a => a.ExceptionRuntimeMessage);
            var messageQuery = getCollectionOrContainuationQuery(pagination.MessageContains, pagination.Messages, a => a.Message);
            var requestPathQuery = getCollectionOrContainuationQuery(pagination.RequestPathContains, pagination.RequestPaths, a => a.RequestPath);

            var fromUtcQuery = pagination.FromUtc != null ?
                new Func<LogRecord, bool>(c => DateTime.TryParse(c.CreatedOnUtc, out DateTime value) && value.ToUniversalTime() >= pagination.FromUtc) :
                c => true;

            var toUtcQuery = pagination.ToUtc != null ?
                 new Func<LogRecord, bool>(c =>
                 {
                     DateTime.TryParse(c.CreatedOnUtc, out DateTime value);
                     return value.ToUniversalTime() <= pagination.ToUtc;
                 }) :
                 c => true;

            return x =>
                logRecordIdQuery(x) &&
                logLevelQuery(x) &&
                userIdQuery(x) &&
                clientIdQuery(x) &&
                exceptionIdQuery(x) &&
                exceptionRuntimeTypeQuery(x) &&
                ipAddressQuery(x) &&
                httpMethodQuery(x) &&
                exceptionRuntimeMessageQuery(x) &&
                messageQuery(x) &&
                requestPathQuery(x) &&
                fromUtcQuery(x) &&
                toUtcQuery(x);

            Func<LogRecord, bool> getCollectionOrContainuationQuery(IEnumerable<string> contains, IEnumerable<string> collection, Func<LogRecord, string> propertyValue)
            {
                return contains.IsNullOrEmpty() ?
                   getCollectionQuery(collection, propertyValue) :
                    new Func<LogRecord, bool>(c =>
                    {
                        var value = propertyValue(c);
                        if (!value.HasValue())
                            return false;

                        var r = contains.Any(a => value.Contains(a));
                        return r;
                    });
            }
            Func<LogRecord, bool> getCollectionQuery(IEnumerable<string> collection, Func<LogRecord, string> propertyValue)
            {
                return collection.IsNullOrEmpty() ?
                    c => true :
                    new Func<LogRecord, bool>(c => collection.Contains(propertyValue(c)));
            }
        }
    }
}
