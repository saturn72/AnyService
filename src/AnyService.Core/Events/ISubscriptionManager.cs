using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public interface ISubscriptionManager
    {
        Task<string> Subscribe(string eventKey, Func<Event, IServiceProvider, Task> handler, string name);
        Task Unsubscribe(string handlerId);
        Task<IEnumerable<HandlerData>> GetAllHandlers();
        Task<IEnumerable<HandlerData>> GetHandlers(string eventKey);
        void Clear();
    }
}