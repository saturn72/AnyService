using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnyService.Security;
using AnyService.Events;
using Microsoft.Extensions.DependencyInjection;

namespace AnyService.Services.Security
{
    public sealed class DefaultPermissionsEventsHandler : IPermissionEventsHandler
    {
        private static readonly object lockObj = new object();
        private readonly IServiceProvider _serviceProvider;

        public DefaultPermissionsEventsHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public Func<DomainEventData, Task> EntityCreatedHandler => async (eventData) =>
          {
              if (!(eventData.Data is IEntity createdEntity))
                  return;

              var manager = _serviceProvider.GetService<IPermissionManager>();
              var userId = eventData.PerformedByUserId;

              var ecr = eventData.WorkContext.CurrentEntityConfigRecord;
              var entityPermission = new EntityPermission
              {
                  EntityId = createdEntity.Id,
                  EntityKey = ecr.EntityKey,
                  PermissionKeys = new[] { ecr.PermissionRecord.ReadKey, ecr.PermissionRecord.UpdateKey, ecr.PermissionRecord.DeleteKey, },
              };

              var userPermissions = new UserPermissions
              {
                  UserId = userId
              };

              lock (lockObj)
              {
                  var temp = manager.GetUserPermissions(userId).Result;
                  var isUpdate = temp != null;
                  if (isUpdate)
                      userPermissions = temp;

                  var eps = userPermissions.EntityPermissions?.ToList() ?? new List<EntityPermission>();
                  eps.Add(entityPermission);
                  userPermissions.EntityPermissions = eps;

                  if (!isUpdate)
                  {
                      var up = manager.CreateUserPermissions(userPermissions).Result;
                      return;
                  }
              }
              await manager.UpdateUserPermissions(userPermissions);
          };


        public Func<DomainEventData, Task> EntityDeletedHandler => async (eventData) =>
      {
          var deletedEntity = eventData.Data as IEntity;
          if (deletedEntity == null)
              return;

          var manager = _serviceProvider.GetService<IPermissionManager>();
          var userId = eventData.PerformedByUserId;
          var ecr = eventData.WorkContext.CurrentEntityConfigRecord;

          var userPermissions = await manager.GetUserPermissions(userId);
          var permissionToDelete = userPermissions?.EntityPermissions?.FirstOrDefault(p => p.EntityId == deletedEntity.Id && p.EntityKey == ecr.EntityKey);
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