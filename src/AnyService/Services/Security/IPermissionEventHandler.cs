using System;
using System.Threading.Tasks;
using AnyService.Events;

namespace AnyService.Services.Security
{
    public interface IPermissionEventsHandler
    {
        Func<Event, IServiceProvider, Task> PermissionCreatedHandler { get; }
        Func<Event, IServiceProvider, Task> PermissionDeletedHandler { get; }
    }
}