using AnyService.Audity;
using System;
using System.Threading.Tasks;

namespace AnyService.Services.Audit
{
    public interface IAuditService
    {
        Task<AuditRecord> InsertAuditRecord(Type entityType, string entityId, string auditRecordType, object data);
        Task<ServiceResponse> GetAll(AuditPagination pagination);
    }
}
