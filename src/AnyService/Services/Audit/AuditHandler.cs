﻿using AnyService.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
            var entities = entity == null ? ded.Data as IEnumerable<IEntity> : new[] { entity };
            return InsertAuditRecord(a => a.InsertCreateRecords(entities, ded.WorkContext, null), entity.ToJsonString());

        };
        public Func<DomainEvent, Task> ReadEventHandler => ded =>
        {
            var entity = ded.Data as IEntity;
            if (entity != null)
                return InsertAuditRecord(a => a.InsertReadRecords(new[] { entity }, ded.WorkContext, null), entity.ToJsonString());

            var page = ded.Data as Pagination;
            var entities = page.DataObject as IEnumerable<IEntity>;
            return InsertAuditRecord(a => a.InsertReadRecords(entities, ded.WorkContext, ToAuditPagination()), null);

            object ToAuditPagination()
            {
                return new
                {
                    Data = page.DataObject,
                    page.IncludeNested,
                    page.Offset,
                    page.OrderBy,
                    page.PageSize,
                    page.QueryOrFilter,
                    page.SortOrder,
                    page.Total,
                    Type = page.Type.FullName,
                };
            }
        };
        public Func<DomainEvent, Task> UpdateEventHandler => ded =>
        {
            var data = (ded.Data as EntityUpdatedDomainEvent)?.Data as EntityUpdatedDomainEvent.EntityUpdatedEventData;
            return InsertAuditRecord(a => a.InsertUpdatedRecord(data.Before, data.After, ded.WorkContext, null), data.ToJsonString());
        };
        public Func<DomainEvent, Task> DeleteEventHandler => ded =>
         {
             var entity = ded.Data as IEntity;
             var entities = entity == null ? ded.Data as IEnumerable<IEntity> : new[] { entity };
             return InsertAuditRecord(a => a.InsertDeletedRecord(entities, ded.WorkContext, null), entity.ToJsonString());
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
