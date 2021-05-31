using System;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public interface ICrossDomainEventSubscriber
    {
        Task<string> Subscribe(string exchange, string routingKey, Func<IntegrationEvent, IServiceProvider, Task> handlerSink, string alias);
        Task Unsubscribe(string handlerId);
    }
}