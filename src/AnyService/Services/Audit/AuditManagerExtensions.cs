using System.Threading.Tasks;

namespace AnyService.Services.Audit
{
    public static class AuditManagerExtensions
    {
        public static Task InsertCreateRecord<TEntity>(this IAuditManager auditHelper, TEntity entity, WorkContext workContext) where TEntity : IEntity
        {
            return auditHelper.InsertAuditRecord(entity.GetType(), entity.Id, AuditRecordTypes.CREATE, workContext, entity);
        }
        public static Task InsertReadRecord<TEntity>(this IAuditManager auditHelper, TEntity entity, WorkContext workContext) where TEntity : IEntity
        {
            return auditHelper.InsertAuditRecord(entity.GetType(), entity.Id, AuditRecordTypes.READ, workContext, entity);
        }
        public static Task InsertReadRecord<TEntity>(this IAuditManager auditHelper, Pagination<TEntity> page, WorkContext workContext) where TEntity : IEntity
        {
            var p = new
            {
                total = page.Total,
                offset = page.Offset,
                pageSize = page.PageSize,
                sortOrder = page.SortOrder,
                orderBy = page.OrderBy,
                data = page.Data,
                queryOrFilter = page.QueryOrFilter,
                includeNested = page.IncludeNested
            };
            return auditHelper.InsertAuditRecord(page.Type, null, AuditRecordTypes.READ, workContext, p);
        }

        public static Task InsertUpdatedRecord<TEntity>(this IAuditManager auditHelper, TEntity before, TEntity after, WorkContext workContext) where TEntity : IEntity
        {
            return auditHelper.InsertAuditRecord(before.GetType(), before.Id, AuditRecordTypes.UPDATE, workContext, new { before, after });
        }
        public static Task InsertDeletedRecord<TEntity>(this IAuditManager auditHelper, TEntity entity, WorkContext workContext) where TEntity : IEntity
        {
            return auditHelper.InsertAuditRecord(entity.GetType(), entity.Id, AuditRecordTypes.DELETE, workContext, entity);
        }
    }
}
