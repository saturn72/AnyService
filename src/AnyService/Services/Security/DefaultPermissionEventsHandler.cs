using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnyService.Core;
using AnyService.Core.Security;
using AnyService.Events;
using Microsoft.Extensions.DependencyInjection;

namespace AnyService.Services.Security
{
    public sealed class DefaultPermissionsEventsHandler : IPermissionEventsHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultPermissionsEventsHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public Action<DomainEventData> EntityCreatedHandler => (eventData) =>
         {
             var manager = _serviceProvider.GetService<IPermissionManager>();
             var userId = eventData.PerformedByUserId;
             var createdEntity = eventData.Data as IDomainModelBase;
             var tcr = TypeConfigRecordManager.GetRecord(createdEntity.GetType());

             var entityPermission = new EntityPermission
             {
                 EntityId = createdEntity.Id,
                 EntityKey = tcr.EntityKey,
                 PermissionKeys = new[] { tcr.PermissionRecord.ReadKey, tcr.PermissionRecord.UpdateKey, tcr.PermissionRecord.DeleteKey, },
             };

             Task.Run(async () =>
             {
                 var userPermissions = await manager.GetUserPermissions(userId);
                 var eps = userPermissions.EntityPermissions?.ToList() ?? new List<EntityPermission>();

                 eps.Add(entityPermission);
                 userPermissions.EntityPermissions = eps.ToArray();
                 await manager.UpdateUserPermissions(userPermissions);
             });
         };
        public Action<DomainEventData> EntityDeletedHandler => (eventData) =>
     {
         var manager = _serviceProvider.GetService<IPermissionManager>();
         var userId = eventData.PerformedByUserId;
         var deletedEntity = eventData.Data as IDomainModelBase;
         var tcr = TypeConfigRecordManager.GetRecord(deletedEntity.GetType());

         Task.Run(async () =>
         {
             var userPermissions = await manager.GetUserPermissions(userId);
             var permissionToDelete = userPermissions.EntityPermissions?.FirstOrDefault(p => p.EntityId == deletedEntity.Id);
             if (permissionToDelete != null)
             {
                 var allUserPermissions = userPermissions.EntityPermissions.ToList();
                 allUserPermissions.Remove(permissionToDelete);
                 userPermissions.EntityPermissions = allUserPermissions.ToArray();
                 await manager.UpdateUserPermissions(userPermissions);
             }
         });
     };
    }
}