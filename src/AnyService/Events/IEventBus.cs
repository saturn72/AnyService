using System;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public interface IEventBus
    {
        void Publish(string eventKey, DomainEventData @event);
        string Subscribe(string eventKey, Func<DomainEventData, Task> handler, string name);
        void Unsubscribe(string handlerId);
    }
}