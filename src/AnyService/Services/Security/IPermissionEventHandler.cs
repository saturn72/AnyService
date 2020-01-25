using System;
using AnyService.Events;

namespace AnyService.Services.Security
{
    public interface IPermissionEventsHandler
    {
        Action<DomainEventData> EntityCreatedHandler { get; }
        Action<DomainEventData> EntityDeletedHandler { get; }
    }
}