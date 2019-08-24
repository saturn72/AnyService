using System.Collections.Generic;

namespace AnyService.Audity
{
    public static class AuditHelperExtensions
    {
        public static void PrepareForCreate(this AuditHelper auditHelper, IEnumerable<ICreatableAudit> audits, string userId)
        {
            foreach (var a in audits)
                auditHelper.PrepareForCreate(a, userId);
        }
    }
}