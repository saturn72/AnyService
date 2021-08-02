using AnyService.Audity;
using AnyService.Events;
using Microsoft.AspNetCore.Authentication;
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
            ISystemClock systemClock,
            ILogger<AuditManager> logger)
            : base(
                  repository,
                  auditConfig,
                  entityConfigRecords,
                  eventBus,
                  systemClock,
                  logger)
        {
        }

        public override Task<IEnumerable<AuditRecord>> Insert(IEnumerable<AuditRecord> records)
        {
            return Task.FromResult(null as IEnumerable<AuditRecord>);
        }
    }
}
