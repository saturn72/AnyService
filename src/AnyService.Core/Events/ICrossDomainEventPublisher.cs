using System.Threading.Tasks;

namespace AnyService.Events
{
    public interface ICrossDomainEventPublisher
    {
        Task Publish(IntegrationEvent @event);
    }
}