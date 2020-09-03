using System.Threading.Tasks;

namespace AnyService.Services.Audit
{
    public interface IAuditHelper
    {
        Task InsertAuditRecord(string entityName, string entityId, string auditRecordCode, object data);
    }
}
