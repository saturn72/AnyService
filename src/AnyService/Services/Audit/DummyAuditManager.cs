using AnyService.Audity;
using AnyService.Events;
using Microsoft.Extensions.Logging;
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
            IDomainEventBus eventBus,
            ILogger<AuditManager> logger)
            : base(
                  repository,
                  auditConfig,
                  entityConfigRecords,
                  eventBus,
                  logger)
        {
        }

        public override Task<IEnumerable<AuditRecord>> Insert(IEnumerable<AuditRecord> records)
        {
            return Task.FromResult(null as IEnumerable<AuditRecord>);
        }
    }
}
