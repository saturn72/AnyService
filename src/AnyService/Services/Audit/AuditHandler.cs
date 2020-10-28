using AnyService.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
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
            return InsertAuditRecord(a => a.InsertCreateRecord(entity, ded.WorkContext), entity);
        };

        public Func<DomainEvent, Task> ReadEventHandler => ded =>
        {
            var entity = ded.Data as IEntity;
            var type = ded.Data.GetType();
            var entityId = entity?.Id;
            return InsertAuditRecord(a => a.InsertAuditRecord(type, entityId, AuditRecordTypes.READ, ded.WorkContext, ded.Data), ded.Data);
        };
        public Func<DomainEvent, Task> UpdateEventHandler => ded =>
        {
            var data = (ded.Data as EntityUpdatedDomainEvent)?.Data as EntityUpdatedDomainEvent.EntityUpdatedEventData;
            var entity = ded.Data as IEntity;

            var type = data?.Before.GetType() ?? ded.Data.GetType();
            var entityId = data?.Before?.Id ?? entity?.Id;

            return InsertAuditRecord(a => a.InsertAuditRecord(type, entityId, AuditRecordTypes.UPDATE, ded.WorkContext, ded.Data), ded.Data);
        };

        public Func<DomainEvent, Task> DeleteEventHandler => ded =>
         {
             var entity = ded.Data as IEntity;
             return InsertAuditRecord(a => a.InsertDeletedRecord(entity, ded.WorkContext), entity);
         };

        private async Task InsertAuditRecord(Func<IAuditManager, Task> action, object data)
        {
            var json = data is Pagination ? data.GetType().Name : data.ToJsonString();
            using var scope = _serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetService<ILogger<AuditHandler>>();
            logger.LogInformation($"Start insertion of audit record. entity = {json}");
            var am = scope.ServiceProvider.GetService<IAuditManager>();
            await action(am);
            logger.LogDebug($"End insertion of audit record.");
        }
    }
}
