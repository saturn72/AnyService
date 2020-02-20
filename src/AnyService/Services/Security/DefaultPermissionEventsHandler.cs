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
        public Func<DomainEventData, Task> EntityCreatedHandler => async (eventData) =>
          {
              var createdEntity = eventData.Data as IDomainModelBase;
              if (createdEntity == null)
                  return;

              var manager = _serviceProvider.GetService<IPermissionManager>();
              var userId = eventData.PerformedByUserId;

              var tcr = EntityConfigRecordManager.GetRecord(createdEntity.GetType());

              var entityPermission = new EntityPermission
              {
                  EntityId = createdEntity.Id,
                  EntityKey = tcr.EntityKey,
                  PermissionKeys = new[] { tcr.PermissionRecord.ReadKey, tcr.PermissionRecord.UpdateKey, tcr.PermissionRecord.DeleteKey, },
              };

              var isUpdate = true;
              var userPermissions = await manager.GetUserPermissions(userId);
              if (userPermissions == null)
              {
                  isUpdate = false;
                  userPermissions = new UserPermissions
                  {
                      UserId = userId
                  };
              }

              var eps = userPermissions.EntityPermissions?.ToList() ?? new List<EntityPermission>();
              eps.Add(entityPermission);
              userPermissions.EntityPermissions = eps.ToArray();

              if (isUpdate)
                  await manager.UpdateUserPermissions(userPermissions);
              else
                  await manager.CreateUserPermissions(userPermissions);
          };


        public Func<DomainEventData, Task> EntityDeletedHandler => async (eventData) =>
      {
          var deletedEntity = eventData.Data as IDomainModelBase;
          if (deletedEntity == null)
              return;

          var manager = _serviceProvider.GetService<IPermissionManager>();
          var userId = eventData.PerformedByUserId;
          var tcr = EntityConfigRecordManager.GetRecord(deletedEntity.GetType());

          var userPermissions = await manager.GetUserPermissions(userId);
          var permissionToDelete = userPermissions?.EntityPermissions?.FirstOrDefault(p => p.EntityId == deletedEntity.Id && p.EntityKey == tcr.EntityKey);
          if (permissionToDelete != null)
          {
              var allUserPermissions = userPermissions.EntityPermissions.ToList();
              allUserPermissions.Remove(permissionToDelete);
              userPermissions.EntityPermissions = allUserPermissions.ToArray();
              await manager.UpdateUserPermissions(userPermissions);
          }
      };
    }
}