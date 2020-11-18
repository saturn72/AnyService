using AnyService.Audity;
using AnyService.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnyService.Services.Audit
{
    public class DummyAuditManager : AuditManager
    {
        public DummyAuditManager(
            IRepository<AuditRecord> repository,
            AuditSettings auditConfig,
            IEnumerable<EntityConfigRecord> entityConfigRecords,
            IEventBus eventBus,
            ILogger<AuditManager> logger)
            : base(
                  repository,
                  auditConfig,
                  entityConfigRecords,
                  eventBus,
                  logger)
        {
        }

        public override Task<AuditRecord> InsertAuditRecord(Type entityType, string entityId, string auditRecordType, WorkContext workcontext, object data)
        {
            return Task.FromResult(null as AuditRecord);
        }
    }
}
