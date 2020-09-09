using AnyService.Audity;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AnyService.Services.Audit
{
    public class DummyAuditManager : AuditManager
    {
        public DummyAuditManager(WorkContext workContext, IRepository<AuditRecord> repository, AuditSettings auditConfig, ILogger<AuditManager> logger) : base(workContext, repository, auditConfig, logger)
        {
        }

        public override Task<AuditRecord> InsertAuditRecord(Type entityType, string entityId, string auditRecordType, object data)
        {
            return Task.FromResult(null as AuditRecord);
        }
    }
}
