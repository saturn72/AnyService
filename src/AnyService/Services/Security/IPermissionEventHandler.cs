using System;
using System.Threading.Tasks;
using AnyService.Events;

namespace AnyService.Services.Security
{
    public interface IPermissionEventsHandler
    {
        Func<DomainEvent, Task> PermissionCreatedHandler { get; }
        Func<DomainEvent, Task> PermissionDeletedHandler { get; }
    }
}