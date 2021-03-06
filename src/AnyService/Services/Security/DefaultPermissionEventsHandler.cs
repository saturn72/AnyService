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
        public Func<DomainEvent, IServiceProvider, Task> PermissionCreatedHandler => async (de, services) =>
          {
              if (!(de.Data is IEntity createdEntity))
                  return;

              var manager = services.GetService<IPermissionManager>();
              var userId = de.PerformedByUserId;

              var ecr = de.WorkContext.CurrentEntityConfigRecord;
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
        public Func<DomainEvent, IServiceProvider, Task> PermissionDeletedHandler => async (de, services) =>
      {
          var deletedEntity = de.Data as IEntity;
          if (deletedEntity == null)
              return;

          var manager = services.GetService<IPermissionManager>();
          var userId = de.PerformedByUserId;
          var ecr = de.WorkContext.CurrentEntityConfigRecord;

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