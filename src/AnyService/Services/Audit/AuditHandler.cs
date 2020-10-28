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
            return InsertAuditRecord(a => a.InsertCreateRecord(entity), entity);
        };

        public Func<DomainEvent, Task> ReadEventHandler => ded =>
        {
            var entity = ded.Data as IEntity;
            var type = ded.Data.GetType();
            var entityId = entity?.Id;
            return InsertAuditRecord(a => a.InsertAuditRecord(type, entityId, AuditRecordTypes.READ, ded.Data), ded.Data);
        };
        public Func<DomainEvent, Task> UpdateEventHandler => ded =>
        {
            var entity = ded.Data as IEntity;
            var data = ded.Data as EntityUpdatedDomainEvent<IEntity>.EntityUpdatedEventData;

            var type = ded.Data.GetType();
            var entityId = entity?.Id ?? data?.Before?.Id;

            return InsertAuditRecord(a => a.InsertAuditRecord(type, entityId, AuditRecordTypes.UPDATE, ded.Data), ded.Data);
        };

        public Func<DomainEvent, Task> DeleteEventHandler => ded =>
         {
             var entity = ded.Data as IEntity;
             return InsertAuditRecord(a => a.InsertDeletedRecord(entity), entity);
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
