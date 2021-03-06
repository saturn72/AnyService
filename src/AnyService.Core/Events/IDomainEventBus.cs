using System;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public interface IDomainEventBus
    {
        Task Publish(string eventKey, Event @event);
        Task<string> Subscribe(string eventKey, Func<Event, IServiceProvider, Task> handler, string name);
        Task Unsubscribe(string handlerId);
    }
}