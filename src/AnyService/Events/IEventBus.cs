using System;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public interface IEventBus
    {
        void Publish(string eventKey, DomainEvent @event);
        string Subscribe(string eventKey, Func<DomainEvent, Task> handler, string name);
        void Unsubscribe(string handlerId);
    }
}