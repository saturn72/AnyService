using AnyService.Audity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnyService.Services.Audit
{
    public interface IAuditManager
    {
        Task<IEnumerable<AuditRecord>> Insert(IEnumerable<AuditRecord> records);
        Task<ServiceResponse<AuditPagination>> GetAll(AuditPagination pagination);
    }
}
