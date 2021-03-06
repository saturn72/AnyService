using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public interface ISubscriptionManager<TEvent>
    {
        Task<string> Subscribe(string eventKey, Func<TEvent, IServiceProvider, Task> handler, string name);
        Task Unsubscribe(string handlerId);
        Task<IEnumerable<HandlerData<TEvent>>> GetAllHandlers();
        Task<IEnumerable<HandlerData<TEvent>>> GetHandlers(string eventKey);
        void Clear();
    }
}