using AnyService.Audity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnyService.Services.Audit
{
    public class DummyAuditManager : AuditManager
    {
        public DummyAuditManager(
            WorkContext workContext,
            IRepository<AuditRecord> repository,
            AuditSettings auditConfig,
            IEnumerable<EntityConfigRecord> entityConfigRecords,
            ILogger<AuditManager> logger) : base(workContext, repository, auditConfig, entityConfigRecords, logger)
        {
        }

        public override Task<AuditRecord> InsertAuditRecord(Type entityType, string entityId, string auditRecordType, object data)
        {
            return Task.FromResult(null as AuditRecord);
        }
    }
}
