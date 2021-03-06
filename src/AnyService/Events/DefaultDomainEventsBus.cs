using AnyService.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public class DefaultDomainEventsBus : IDomainEventBus
    {
        private const int TaskExecutionTimeout = 60000;
        private readonly ISubscriptionManager<DomainEvent> _subscriptionManager;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DefaultDomainEventsBus> _logger;
        public DefaultDomainEventsBus(
            ISubscriptionManager<DomainEvent> subscriptionManager,
            IServiceProvider serviceProvider,
            ILogger<DefaultDomainEventsBus> logger)
        {
            _subscriptionManager = subscriptionManager;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        public async Task Publish(string eventKey, DomainEvent @event)
        {
            _logger.LogInformation(LoggingEvents.EventPublishing, $"Publishing event with key: {eventKey}");

            var handlers = await _subscriptionManager.GetHandlers(eventKey);
            if (handlers.IsNullOrEmpty())
                return;

            var tasks = new List<Task>();
            using var scope = _serviceProvider.CreateScope();
            foreach (var h in handlers)
            {
                _logger.LogInformation(LoggingEvents.EventPublishing, $"Publishing event to handler named {h.Name}");
                tasks.Add(h.Handler(@event, scope.ServiceProvider));
            }
            if (!Task.WaitAll(tasks.ToArray(), TaskExecutionTimeout))
                _logger.LogDebug($"Not all tasks finished execution within given timout ({TaskExecutionTimeout})");
        }
        public Task<string> Subscribe(string eventKey, Func<DomainEvent, IServiceProvider, Task> handler, string name) =>
            _subscriptionManager.Subscribe(eventKey, handler, name);
        public Task Unsubscribe(string handlerId) => _subscriptionManager.Unsubscribe(handlerId);
    }
}