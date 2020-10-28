using AnyService.Audity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Services.Audit
{
    public class AuditManager : IAuditManager
    {
        #region Fields
        private static readonly ConcurrentDictionary<Type, string> EntityTypesNames = new ConcurrentDictionary<Type, string>();
        private readonly IRepository<AuditRecord> _repository;
        private readonly AuditSettings _auditSettings;
        private readonly IEnumerable<EntityConfigRecord> _entityConfigRecords;
        private readonly ILogger<AuditManager> _logger;
        private static readonly IEnumerable<string> FaultedServiceResult = new[] { ServiceResult.BadOrMissingData, ServiceResult.Error, ServiceResult.NotFound };
        #endregion

        #region ctor
        public AuditManager(
            IRepository<AuditRecord> repository,
            AuditSettings auditConfig,
            IEnumerable<EntityConfigRecord> entityConfigRecords,
            ILogger<AuditManager> logger
            )
        {
            _repository = repository;
            _auditSettings = auditConfig;
            _entityConfigRecords = entityConfigRecords;
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

            var isFault = FaultedServiceResult.Contains(wSrvRes.Result);

            pagination.Data = isFault ? data : (data ?? new AuditRecord[] { });
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
            var auditRecordsQuery = getCollectionQuery(pagination.AuditRecordTypes, a => a.AuditRecordType);
            var entityNamesQuery = getCollectionQuery(pagination.EntityNames, a => a.EntityName);
            var userIdsQuery = getCollectionQuery(pagination.UserIds, a => a.UserId);
            var clientIdsQuery = getCollectionQuery(pagination.ClientIds, a => a.ClientId);

            var fromUtcQuery = pagination.FromUtc != null ?
                new Func<AuditRecord, bool>(c => DateTime.TryParse(c.CreatedOnUtc, out DateTime value) && value.ToUniversalTime() >= pagination.FromUtc) :
                null;

            var toUtcQuery = pagination.ToUtc != null ?
                new Func<AuditRecord, bool>(c =>
                {
                    DateTime.TryParse(c.CreatedOnUtc, out DateTime value);
                    return value.ToUniversalTime() <= pagination.ToUtc;
                }) : null;
            var q = auditRecordIds.AndAlso(
                    entityIdsQuery,
                    auditRecordsQuery,
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
        public async virtual Task<AuditRecord> InsertAuditRecord(Type entityType, string entityId, string auditRecordType, WorkContext workContext, object data)
        {
            if (!ShouldAudit(auditRecordType))
                return null;
            var record = new AuditRecord
            {
                EntityName = GetEntityName(entityType),
                EntityId = entityId,
                AuditRecordType = auditRecordType,
                Data = data.ToJsonString(),
                WorkContext = workContext.Parameters?.ToJsonString(),
                UserId = workContext.CurrentUserId,
                ClientId = workContext.CurrentClientId,
                CreatedOnUtc = DateTime.UtcNow.ToIso8601(),
            };
            return await _repository.Insert(record);
        }

        private bool ShouldAudit(string auditRecordType)
        {
            return
                (auditRecordType == AuditRecordTypes.CREATE && _auditSettings.AuditRules.AuditCreate) ||
                (auditRecordType == AuditRecordTypes.READ && _auditSettings.AuditRules.AuditRead) ||
                (auditRecordType == AuditRecordTypes.UPDATE && _auditSettings.AuditRules.AuditUpdate) ||
                (auditRecordType == AuditRecordTypes.DELETE && _auditSettings.AuditRules.AuditDelete) ||
                false;
        }
        private string GetEntityName(Type entityType)
        {
            if (EntityTypesNames.TryGetValue(entityType, out string value))
                return value;

            EntityTypesNames.TryAdd(entityType, _entityConfigRecords.First(entityType).Name);
            return EntityTypesNames[entityType];
        }
    }
}
