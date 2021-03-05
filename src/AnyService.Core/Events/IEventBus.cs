using System;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public interface IEventBus
    {
        void Publish(string eventKey, Event @event);
        string Subscribe(string eventKey, Func<Event, IServiceProvider, Task> handler, string name);
        void Unsubscribe(string handlerId);
    }
}