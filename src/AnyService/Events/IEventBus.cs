using System;

namespace AnyService.Events
{
    public interface IEventBus
    {
        void Publish(string eventKey, EventData @event);
        string Subscribe(string eventKey, Action<EventData> handler);
        void Unsubscribe(string handlerId);
    }
}