using System.Threading.Tasks;

namespace AnyService.Events
{
    public interface ICrossDomainEventPublishManager
    {
        Task PublishToAll(IntegrationEvent @event);
    }
}