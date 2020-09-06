using System.Threading.Tasks;

namespace AnyService.Services.Audit
{
    public static class AuditServiceExtensions
    {
        public static Task InsertCreateRecord<TEntity>(this IAuditService auditHelper, TEntity entity) where TEntity : IDomainModelBase
        {
            return auditHelper.InsertAuditRecord(typeof(TEntity), entity.Id, AuditRecordTypes.CREATE, entity);
        }
        public static Task InsertReadRecord<TEntity>(this IAuditService auditHelper, TEntity entity) where TEntity : IDomainModelBase
        {
            return auditHelper.InsertAuditRecord(typeof(TEntity), entity.Id, AuditRecordTypes.READ, entity);
        }
        public static Task InsertReadRecord<TEntity>(this IAuditService auditHelper, Pagination<TEntity> page) where TEntity : IDomainModelBase
        {
            return auditHelper.InsertAuditRecord(typeof(TEntity), null, AuditRecordTypes.READ, page);
        }

        public static Task InsertUpdatedRecord<TEntity>(this IAuditService auditHelper, TEntity before, TEntity after) where TEntity : IDomainModelBase
        {
            return auditHelper.InsertAuditRecord(typeof(TEntity), before.Id, AuditRecordTypes.UPDATE, new { before, after });
        }
        public static Task InsertDeletedRecord<TEntity>(this IAuditService auditHelper, TEntity entity) where TEntity : IDomainModelBase
        {
            return auditHelper.InsertAuditRecord(typeof(TEntity), entity.Id, AuditRecordTypes.DELETE, entity);
        }
    }
}
