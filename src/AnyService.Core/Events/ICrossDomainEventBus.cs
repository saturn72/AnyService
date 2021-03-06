using System;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public interface ICrossDomainEventBus
    {
        Task Publish(string eventKey, IntegrationEvent @event);
        Task<string> Subscribe(string eventKey, Func<IntegrationEvent, IServiceProvider, Task> handler, string name);
        Task Unsubscribe(string handlerId);
    }
}