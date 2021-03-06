using System;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public interface IDomainEventBus
    {
        Task Publish(string eventKey, DomainEvent @event);
        Task<string> Subscribe(string eventKey, Func<DomainEvent, IServiceProvider, Task> handler, string name);
        Task Unsubscribe(string handlerId);
    }
}