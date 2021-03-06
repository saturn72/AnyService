using System;
using System.Threading.Tasks;
using AnyService.Events;

namespace AnyService.Services.Security
{
    public interface IPermissionEventsHandler
    {
        Func<DomainEvent, IServiceProvider, Task> PermissionCreatedHandler { get; }
        Func<DomainEvent, IServiceProvider, Task> PermissionDeletedHandler { get; }
    }
}