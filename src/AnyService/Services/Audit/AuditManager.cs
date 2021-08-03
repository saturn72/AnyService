using AnyService.Audity;
using AnyService.Events;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Services.Audit
{
    public class AuditManager : IAuditManager
    {
        #region Fields
        private readonly IRepository<AuditRecord> _repository;
        private readonly IDomainEventBus _eventBus;
        private readonly ISystemClock _systemClock;
        private readonly ILogger<AuditManager> _logger;
        private readonly IEnumerable<EntityConfigRecord> _entityConfigRecords;
        private readonly EventKeyRecord _auditEventKeys;
        private readonly IEnumerable<string> _faultedServiceResult = new[] { ServiceResult.BadOrMissingData, ServiceResult.Error, ServiceResult.NotFound };
        #endregion
        #region ctor
        public AuditManager(
            IRepository<AuditRecord> repository,
            IEnumerable<EntityConfigRecord> entityConfigRecords,
            IDomainEventBus eventBus,
            ISystemClock systemClock,
            ILogger<AuditManager> logger)
        {
            _repository = repository;
            _eventBus = eventBus;
            _systemClock = systemClock;
            _entityConfigRecords = entityConfigRecords;
            _auditEventKeys = entityConfigRecords.First(typeof(AuditRecord)).EventKeys;

            _logger = logger;
        }
        #endregion
        public async virtual Task<ServiceResponse<AuditPagination>> GetAll(AuditPagination pagination)
        {
            _logger.LogInformation(LoggingEvents.BusinessLogicFlow, "Start get all audit records flow");
            pagination.QueryFunc = BuildAuditPaginationQuery(pagination);

            _logger.LogDebug(LoggingEvents.Repository, "Get all audit-records from repository using paginate = " + pagination);

            var wrapper = new ServiceResponseWrapper(new ServiceResponse<IEnumerable<AuditRecord>>());
            var data = await _repository.Query(r => r.GetAll(pagination), wrapper);
            _logger.LogDebug(LoggingEvents.Repository, $"Repository response: {data?.ToJsonString()}");
            var wSrvRes = wrapper.ServiceResponse;
            var isFault = _faultedServiceResult.Contains(wSrvRes.Result);
            pagination.Data = isFault ? data : (data ?? Array.Empty<AuditRecord>());
            var serviceResponse = new ServiceResponse<AuditPagination>
            {
                Payload = pagination,
                Message = wSrvRes.Message,
                TraceId = wSrvRes.TraceId,
                Result = isFault ? wSrvRes.Result : ServiceResult.Ok
            };
            _logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Service Response: {serviceResponse}");
            return serviceResponse;
        }

        protected Func<AuditRecord, bool> BuildAuditPaginationQuery(AuditPagination pagination)
        {
            var auditRecordIds = getCollectionQuery(pagination.AuditRecordIds, a => a.Id);
            var entityIdsQuery = getCollectionQuery(pagination.EntityIds, a => a.EntityId);
            var auditRecordsTypeQuery = getCollectionQuery(pagination.AuditRecordTypes, a => a.AuditRecordType);
            var entityNamesQuery = getCollectionQuery(pagination.EntityNames, a => a.EntityName);
            var userIdsQuery = getCollectionQuery(pagination.UserIds, a => a.UserId);
            var clientIdsQuery = getCollectionQuery(pagination.ClientIds, a => a.ClientId);

            var fromUtcQuery = pagination.FromUtc != null ?
                new Func<AuditRecord, bool>(c => DateTime.TryParse(c.CreatedOnUtc, out DateTime value) && value.ToUniversalTime() >= pagination.FromUtc) :
                null;

            var toUtcQuery = pagination.ToUtc != null ?
                new Func<AuditRecord, bool>(c =>
                {
                    _ = DateTime.TryParse(c.CreatedOnUtc, out DateTime value);
                    return value.ToUniversalTime() <= pagination.ToUtc;
                }) : null;
            var q = auditRecordIds.AndAlso(
                    entityIdsQuery,
                    auditRecordsTypeQuery,
                    entityNamesQuery,
                    userIdsQuery,
                    clientIdsQuery,
                    fromUtcQuery,
                    toUtcQuery);
            return q ?? new Func<AuditRecord, bool>(x => true);

            Func<AuditRecord, bool> getCollectionQuery(IEnumerable<string> collection, Func<AuditRecord, string> propertyValue)
            {
                return collection.IsNullOrEmpty() ?
                    null :
                    new Func<AuditRecord, bool>(c => collection.Contains(propertyValue(c)));
            }
        }
        public async virtual Task<IEnumerable<AuditRecord>> Insert(IEnumerable<AuditRecord> records)
        {
            var toInsert = records?.Where(ShouldAudit).ToArray();
            if (toInsert.IsNullOrEmpty()) return Array.Empty<AuditRecord>();

            foreach (var item in toInsert)
                item.CreatedOnUtc = _systemClock.UtcNow.ToIso8601();
            var res = await _repository.BulkInsert(toInsert);
            _ = _eventBus.Publish(_auditEventKeys.Create, new DomainEvent { Data = res });
            return res;
        }
        private bool ShouldAudit(AuditRecord record)
        {
            var auditRecordType = record.AuditRecordType;
            var auditSettings = _entityConfigRecords.First(record.EntityName).AuditSettings;
            return auditSettings.Enabled && (
                (auditRecordType == AuditRecordTypes.CREATE && auditSettings.AuditRules.AuditCreate) ||
                (auditRecordType == AuditRecordTypes.READ && auditSettings.AuditRules.AuditRead) ||
                (auditRecordType == AuditRecordTypes.UPDATE && auditSettings.AuditRules.AuditUpdate) ||
                (auditRecordType == AuditRecordTypes.DELETE && auditSettings.AuditRules.AuditDelete));
        }
    }
}
