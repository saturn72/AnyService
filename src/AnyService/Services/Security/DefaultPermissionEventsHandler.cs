using System;
using AnyService.Events;

namespace AnyService.Services.Security
{
    public sealed class DefaultPermissionsEventHandler : IPermissionEventHandler
    {
        public Action<EventData> EntityCreatedHandler => (eventData) =>
       {
            //Add user Permission of updat, get, delete
            throw new NotImplementedException();
       };
        public Action<EventData> EntityDeletedHandler => (eventData) =>
       {
            //remove all pemissions
            throw new NotImplementedException();
       };
    }
}