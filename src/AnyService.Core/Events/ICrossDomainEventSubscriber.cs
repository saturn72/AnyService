using System;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public interface ICrossDomainEventSubscriber
    { 
        Task<string> Subscribe(string @namespace, string eventKey, Func<IntegrationEvent, IServiceProvider, Task> handler, string alias);
        Task Unsubscribe(string handlerId);
    }
}