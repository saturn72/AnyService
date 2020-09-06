using AnyService.Audity;
using System;
using System.Threading.Tasks;

namespace AnyService.Services.Audit
{
    public interface IAuditManager
    {
        Task<AuditRecord> InsertAuditRecord(Type entityType, string entityId, string auditRecordType, object data);
        Task<ServiceResponse> GetAll(AuditPagination pagination);
    }
}
