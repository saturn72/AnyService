using AnyService.Audity;
using System;
using System.Threading.Tasks;

namespace AnyService.Services.Audit
{
    public interface IAuditManager
    {
        Task<AuditRecord> InsertAuditRecord(Type entityType, string entityId, string auditRecordType, object data);
        Task<ServiceResponse<AuditPagination>> GetAll(AuditPagination pagination);
    }
}
