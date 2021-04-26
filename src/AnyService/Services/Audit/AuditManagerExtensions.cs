using AnyService.Audity;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Concurrent;

namespace AnyService.Services.Audit
{
    public static class AuditManagerExtensions
    {
        private static readonly List<EntityConfigRecord> EntityConfigRecords = new List<EntityConfigRecord>();
        private static readonly ConcurrentDictionary<Type, string> EntityTypesNames = new ConcurrentDictionary<Type, string>();
        public static void AddEntityConfigRecords(IEnumerable<EntityConfigRecord> entityConfigRecords)
        {
            if (!entityConfigRecords.IsNullOrEmpty())
                EntityConfigRecords.AddRange(entityConfigRecords);
        }
        public static Task InsertCreateRecords(this IAuditManager auditHelper, IEnumerable<IEntity> entities, WorkContext workContext, object context)
        {
            var records = ToAuditRecords(entities, AuditRecordTypes.CREATE, workContext, context);
            return auditHelper.Insert(records);
        }
        public static Task InsertReadRecords(this IAuditManager auditHelper, IEnumerable<IEntity> entities, WorkContext workContext, object context)
        {
            var records = ToAuditRecords(entities, AuditRecordTypes.READ, workContext, context);
            return auditHelper.Insert(records);
        }
        public static Task InsertUpdatedRecord(this IAuditManager auditHelper, IEntity before, IEntity after, WorkContext workContext, object context)
        {
            var entity = new { before, after };
            var ar = new AuditRecord
            {
                EntityId = before.Id,
                EntityName = GetEntityName(before.GetType()),
                AuditRecordType = AuditRecordTypes.UPDATE,
                Data = entity.ToJsonString(),
                Context = context.ToJsonString(),
                WorkContext = workContext.Parameters?.ToJsonString(),
                UserId = workContext.CurrentUserId,
                ClientId = workContext.CurrentClientId,
            };
            return auditHelper.Insert(new[] { ar });
        }
        public static Task InsertDeletedRecord(this IAuditManager auditHelper, IEnumerable<IEntity> entities, WorkContext workContext, object context)
        {
            var records = ToAuditRecords(entities, AuditRecordTypes.DELETE, workContext, context);
            return auditHelper.Insert(records);
        }
        private static IEnumerable<AuditRecord> ToAuditRecords(
            IEnumerable<IEntity> entities,
            string auditRecordType,
            WorkContext workContext,
            object context
            )
        {
            return entities.Select(e => new AuditRecord
            {
                EntityId = e.Id,
                EntityName = GetEntityName(e.GetType()),
                AuditRecordType = auditRecordType,
                Data = e.ToJsonString(),
                Context = (context ?? e).ToJsonString(),
                WorkContext = workContext.Parameters?.ToJsonString(),
                UserId = workContext.CurrentUserId,
                ClientId = workContext.CurrentClientId,
            });
        }
        private static string GetEntityName(Type entityType)
        {
            if (EntityTypesNames.TryGetValue(entityType, out string value))
                return value;

            value = EntityConfigRecords.First(entityType).Name;
            EntityTypesNames.TryAdd(entityType, value);
            return EntityTypesNames[entityType];
        }
    }
}
