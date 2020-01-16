using System;
using AnyService.Events;

namespace AnyService.Services.Security
{
    public interface IPermissionEventHandler
    {
        Action<EventData> EntityCreatedHandler { get; }
        Action<EventData> EntityDeletedHandler { get; }
    }
}