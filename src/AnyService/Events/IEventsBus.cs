using System;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public interface IEventsBus
    {
        void Publish(string eventKey, DomainEventData @event);
        string Subscribe(string eventKey, Func<DomainEventData, Task> handler);
        void Unsubscribe(string handlerId);
    }
}