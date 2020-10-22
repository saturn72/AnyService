using System.Threading.Tasks;

namespace AnyService.Services.Audit
{
    public static class AuditManagerExtensions
    {
        public static Task InsertCreateRecord<TEntity>(this IAuditManager auditHelper, TEntity entity) where TEntity : IEntity
        {
            return auditHelper.InsertAuditRecord(typeof(TEntity), entity.Id, AuditRecordTypes.CREATE, entity);
        }
        public static Task InsertReadRecord<TEntity>(this IAuditManager auditHelper, TEntity entity) where TEntity : IEntity
        {
            return auditHelper.InsertAuditRecord(typeof(TEntity), entity.Id, AuditRecordTypes.READ, entity);
        }
        public static Task InsertReadRecord<TEntity>(this IAuditManager auditHelper, Pagination<TEntity> page) where TEntity : IEntity
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
            return auditHelper.InsertAuditRecord(typeof(TEntity), null, AuditRecordTypes.READ, p);
        }

        public static Task InsertUpdatedRecord<TEntity>(this IAuditManager auditHelper, TEntity before, TEntity after) where TEntity : IEntity
        {
            return auditHelper.InsertAuditRecord(typeof(TEntity), before.Id, AuditRecordTypes.UPDATE, new { before, after });
        }
        public static Task InsertDeletedRecord<TEntity>(this IAuditManager auditHelper, TEntity entity) where TEntity : IEntity
        {
            return auditHelper.InsertAuditRecord(typeof(TEntity), entity.Id, AuditRecordTypes.DELETE, entity);
        }
    }
}
