using System.Threading.Tasks;

namespace AnyService.Services.Audit
{
    public static class AuditHelperExtensions
    {
        public static Task InsertCreateRecord<TEntity>(this IAuditHelper auditHelper, TEntity entity) where TEntity : IDomainModelBase
        {
            return auditHelper.InsertAuditRecord(typeof(TEntity).FullName, entity.Id, AuditRecordTypes.CREATE, entity);
        }

        public static Task InsertUpdatedRecord<TEntity>(this IAuditHelper auditHelper, TEntity before, TEntity after) where TEntity : IDomainModelBase
        {
            return auditHelper.InsertAuditRecord(typeof(TEntity).FullName, before.Id, AuditRecordTypes.UPDATE, new { before, after });
        }
        public static Task InsertDeletedRecord<TEntity>(this IAuditHelper auditHelper, TEntity entity) where TEntity : IDomainModelBase
        {
            return auditHelper.InsertAuditRecord(typeof(TEntity).FullName, entity.Id, AuditRecordTypes.DELETE, entity);
        }
    }
}
