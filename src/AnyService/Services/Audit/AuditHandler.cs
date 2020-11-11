using AnyService.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace AnyService.Services.Audit
{
    public class AuditHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public AuditHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public Func<DomainEvent, Task> CreateEventHandler => ded =>
        {
            var entity = ded.Data as IEntity;
            return InsertAuditRecord(a => a.InsertCreateRecord(entity, ded.WorkContext), entity.ToJsonString());
        };
        public Func<DomainEvent, Task> ReadEventHandler => ded =>
        {
            var entity = ded.Data as IEntity;
            var data = ded.Data as Pagination;

            var type = data?.Type ?? ded.Data.GetType();
            var entityId = entity?.Id;
            var entityJson = data?.Type.Name ?? entity.ToJsonString();
            return InsertAuditRecord(a => a.InsertAuditRecord(type, entityId, AuditRecordTypes.READ, ded.WorkContext, ToAuditPagination(data) ?? entity), entityJson);

            object ToAuditPagination(Pagination p)
            {
                return p == null ?
                    null :
                new
                {
                    p.IncludeNested,
                    p.Offset,
                    p.OrderBy,
                    p.PageSize,
                    p.QueryOrFilter,
                    p.SortOrder,
                    p.Total,
                    Type = p.Type.FullName,
                };
            }
        };
        public Func<DomainEvent, Task> UpdateEventHandler => ded =>
        {
            var data = (ded.Data as EntityUpdatedDomainEvent)?.Data as EntityUpdatedDomainEvent.EntityUpdatedEventData;
            var entity = ded.Data as IEntity;

            var type = data?.Before.GetType() ?? ded.Data.GetType();
            var entityId = data?.Before?.Id ?? entity?.Id;

            return InsertAuditRecord(a => a.InsertAuditRecord(type, entityId, AuditRecordTypes.UPDATE, ded.WorkContext, ded.Data), entity.ToJsonString());
        };

        public Func<DomainEvent, Task> DeleteEventHandler => ded =>
         {
             var entity = ded.Data as IEntity;
             return InsertAuditRecord(a => a.InsertDeletedRecord(entity, ded.WorkContext), entity.ToJsonString());
         };

        private async Task InsertAuditRecord(Func<IAuditManager, Task> action, string entityJson)
        {
            using var scope = _serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetService<ILogger<AuditHandler>>();
            logger.LogInformation($"Start insertion of audit record. entity = {entityJson}");
            var am = scope.ServiceProvider.GetService<IAuditManager>();
            await action(am);
            logger.LogDebug($"End insertion of audit record.");
        }
    }
}
