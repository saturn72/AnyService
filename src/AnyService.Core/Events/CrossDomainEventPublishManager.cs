using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public class CrossDomainEventPublishManager : ICrossDomainEventPublisher
    {
        private readonly IEnumerable<ICrossDomainEventPublisher> _publishers;
        private readonly ILogger<CrossDomainEventPublishManager> _logger;

        public CrossDomainEventPublishManager(
            IEnumerable<ICrossDomainEventPublisher> publishers,
            ILogger<CrossDomainEventPublishManager> logger
            )
        {
            _publishers = publishers?.Where(p => p.GetType() != GetType())?.ToArray() ?? Array.Empty<ICrossDomainEventPublisher>();
            _logger = logger;
        }
        public Task Publish(IntegrationEvent @event)
        {
            _logger.LogDebug($"Publishing event: {@event.ToJsonString()}");
            foreach (var p in _publishers)
            {
                _logger.LogDebug($"async publishing using publisher: {p.GetType().Name}");
                _ = p.Publish(@event);
            }
            _logger.LogDebug($"end publishing to all publishers");

            return Task.CompletedTask;
        }
    }
}