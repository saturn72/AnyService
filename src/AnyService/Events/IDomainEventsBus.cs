using System;

namespace AnyService.Events
{
    public interface IDomainEventsBus
    {
        void Publish(string eventKey, DomainEventData @event);
        string Subscribe(string eventKey, Action<DomainEventData> handler);
        void Unsubscribe(string handlerId);
    }
}