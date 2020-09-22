using AnyService.Audity;
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
        private static readonly IDictionary<Type, string> EntityTypesNames = new Dictionary<Type, string>();
        private readonly WorkContext _workContext;
        private readonly IRepository<AuditRecord> _repository;
        private readonly AuditSettings _auditSettings;
        private readonly IEnumerable<EntityConfigRecord> _entityConfigRecords;
        private readonly ILogger<AuditManager> _logger;
        #endregion

        #region ctor
        public AuditManager(
            WorkContext workContext,
            IRepository<AuditRecord> repository,
            AuditSettings auditConfig,
            IEnumerable<EntityConfigRecord> entityConfigRecords,
            ILogger<AuditManager> logger
            )
        {
            _workContext = workContext;
            _repository = repository;
            _auditSettings = auditConfig;
            _entityConfigRecords = entityConfigRecords;
            _logger = logger;
        }
        #endregion
        public async virtual Task<ServiceResponse<AuditPagination>> GetAll(AuditPagination pagination)
        {
            _logger.LogDebug(LoggingEvents.BusinessLogicFlow, "Start get all audit records flow");
            pagination.QueryFunc = BuildAuditPaginationQuery(pagination);

            _logger.LogDebug(LoggingEvents.Repository, "Get all audit-records from repository using paginate = " + pagination);

            var serviceResponse = new ServiceResponse<AuditPagination> { Payload = pagination };
            var wrapper = new ServiceResponseWrapper(new ServiceResponse<IEnumerable<AuditRecord>>());
            var data = await _repository.Query(r => r.GetAll(pagination), wrapper);
            _logger.LogDebug(LoggingEvents.Repository, $"Repository response: {data.ToJsonString()}");
            if (serviceResponse.Result != ServiceResult.NotSet)
                return serviceResponse;

            pagination.Data = data ?? new AuditRecord[] { };
            serviceResponse.Payload = pagination;
            serviceResponse.Result = ServiceResult.Ok;
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
                new Func<AuditRecord, bool>(c => DateTime.Parse(c.OnUtc) >= pagination.FromUtc) :
                c => true;
            var toUtcQuery = pagination.ToUtc != null ?
                new Func<AuditRecord, bool>(c => DateTime.Parse(c.OnUtc) <= pagination.ToUtc) :
                c => true;

            return x =>
                auditRecordIds(x) &&
                entityIdsQuery(x) &&
                auditRecordsQuery(x) &&
                entityNamesQuery(x) &&
                userIdsQuery(x) &&
                clientIdsQuery(x) &&
                fromUtcQuery(x) &&
                toUtcQuery(x);

            Func<AuditRecord, bool> getCollectionQuery(IEnumerable<string> collection, Func<AuditRecord, string> propertyValue)
            {
                return collection.IsNullOrEmpty() ?
                    c => true :
                    new Func<AuditRecord, bool>(c => collection.Contains(propertyValue(c)));
            }
        }

        public async virtual Task<AuditRecord> InsertAuditRecord(Type entityType, string entityId, string auditRecordType, object data)
        {
            if (!ShouldAudit(auditRecordType))
                return null;
            var record = new AuditRecord
            {
                EntityName = GetEntityName(entityType),
                EntityId = entityId,
                AuditRecordType = auditRecordType,
                Data = data.ToJsonString(),
                UserId = _workContext.CurrentUserId,
                ClientId = _workContext.CurrentClientId,
                OnUtc = DateTime.UtcNow.ToIso8601(),
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

            EntityTypesNames[entityType] = _entityConfigRecords.First(entityType).Name ;
            return EntityTypesNames[entityType];
        }
    }
}
