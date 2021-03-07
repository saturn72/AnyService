using System.Threading.Tasks;

namespace AnyService.Events
{
    public interface ICrossDomainEventPublisher
    {
        Task Publish(string @namespace, string eventKey, IntegrationEvent @event);
    }
}