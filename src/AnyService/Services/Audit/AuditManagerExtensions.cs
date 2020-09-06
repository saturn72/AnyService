using System.Threading.Tasks;

namespace AnyService.Services.Audit
{
    public static class AuditManagerExtensions
    {
        public static Task InsertCreateRecord<TEntity>(this IAuditManager auditHelper, TEntity entity) where TEntity : IDomainModelBase
        {
            return auditHelper.InsertAuditRecord(typeof(TEntity), entity.Id, AuditRecordTypes.CREATE, entity);
        }
        public static Task InsertReadRecord<TEntity>(this IAuditManager auditHelper, TEntity entity) where TEntity : IDomainModelBase
        {
            return auditHelper.InsertAuditRecord(typeof(TEntity), entity.Id, AuditRecordTypes.READ, entity);
        }
        public static Task InsertReadRecord<TEntity>(this IAuditManager auditHelper, Pagination<TEntity> page) where TEntity : IDomainModelBase
        {
            return auditHelper.InsertAuditRecord(typeof(TEntity), null, AuditRecordTypes.READ, page);
        }

        public static Task InsertUpdatedRecord<TEntity>(this IAuditManager auditHelper, TEntity before, TEntity after) where TEntity : IDomainModelBase
        {
            return auditHelper.InsertAuditRecord(typeof(TEntity), before.Id, AuditRecordTypes.UPDATE, new { before, after });
        }
        public static Task InsertDeletedRecord<TEntity>(this IAuditManager auditHelper, TEntity entity) where TEntity : IDomainModelBase
        {
            return auditHelper.InsertAuditRecord(typeof(TEntity), entity.Id, AuditRecordTypes.DELETE, entity);
        }
    }
}
