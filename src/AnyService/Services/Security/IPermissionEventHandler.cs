using System;
using System.Threading.Tasks;
using AnyService.Events;

namespace AnyService.Services.Security
{
    public interface IPermissionEventsHandler
    {
        Func<DomainEventData, Task> EntityCreatedHandler { get; }
        Func<DomainEventData, Task> EntityDeletedHandler { get; }
    }
}