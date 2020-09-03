using AnyService.Audity;
using System;
using System.Threading.Tasks;

namespace AnyService.Services.Audit
{
    public class AuditHelper : IAuditHelper
    {
        private readonly WorkContext _workContext;
        private readonly IRepository<AuditRecord> _repository;

        public AuditHelper(
            WorkContext workContext,
            IRepository<AuditRecord> repository
            )
        {
            _workContext = workContext;
            _repository = repository;
        }

        public async Task InsertAuditRecord(string entityName, string entityId, string auditRecordType, object data)
        {
            var record = new AuditRecord
            {
                EntityName = entityName,
                EntityId = entityId,
                AuditRecordType = auditRecordType,
                Data = data.ToJsonString(),
                UserId = _workContext.CurrentUserId,
                ClientId = _workContext.CurrentClientId,
                Iso8601Utc = DateTime.UtcNow.ToIso8601(),
            };
            await _repository.Insert(record);
        }
    }
}
