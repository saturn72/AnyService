using System;
using AnyService.Core.Security;
using AnyService.Events;

namespace AnyService.Services.Security
{
    public sealed class DefaultPermissionsEventsHandler : IPermissionEventsHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultPermissionsEventsHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public Action<EventData> EntityCreatedHandler => (eventData) =>
         {
             var manager = _serviceProvider.GetService<IPermissionManager>();
             var userPermissions = manager.GetUserPermissions(userId);
             update permissions: add permissions related to entity(update, GetHashCode, delete)
                  permissionKey, entityKey, entityId, userId,
             new[] {
                   update-permission,
                   get-permission,
                   delete-permission,
             });
             manager.UpdateUserPermissions(userPermission.Id, userPermissions);
         };
        public Action<EventData> EntityDeletedHandler => (eventData) =>
     {
         var manager = _serviceProvider.GetService<IPermissionManager>();
         var userPermissions = manager.GetUserPermissions(userId);
         remove all permissions related to entity
                  permissionKey, entityKey, entityId, userId,
             new[] {
                   update-permission,
                   get-permission,
                   delete-permission,
             });
         manager.UpdateUserPermissions(userPermission.Id, userPermissions);
     };
    }
}