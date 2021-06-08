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
        private const string DefaultNamespaceConst = "default";
        private const int TaskExecutionTimeout = 60000;
        private readonly ISubscriptionManager<DomainEvent> _subscriptionManager;
        private readonly IServiceProvider _services;
        private readonly ILogger<DefaultDomainEventsBus> _logger;
        private readonly string _defaultNamespace;

        public DefaultDomainEventsBus(
            ISubscriptionManager<DomainEvent> subscriptionManager,
            IServiceProvider services,
            ILogger<DefaultDomainEventsBus> logger,
            string defaultNamespace = DefaultNamespaceConst)
        {
            _subscriptionManager = subscriptionManager;
            _services = services;
            _logger = logger;
            _defaultNamespace = defaultNamespace;
        }
        public async Task Publish(string eventKey, DomainEvent @event)
        {
            _logger.LogInformation(LoggingEvents.EventPublishing, $"Publishing event with key: {eventKey}");

            var handlers = await _subscriptionManager.GetHandlers(_defaultNamespace, eventKey);
            if (handlers.IsNullOrEmpty())
                return;

            await Task.Yield();
            var tasks = new List<Task>();
            using var scope = _services.CreateScope();
            foreach (var h in handlers)
            {
                _logger.LogInformation(LoggingEvents.EventPublishing, $"Publishing event to handler named {h.Alias}");
                tasks.Add(h.Handler(@event, scope.ServiceProvider));
            }
            if (!Task.WaitAll(tasks.ToArray(), TaskExecutionTimeout))
                _logger.LogDebug($"Not all tasks finished execution within given timout ({TaskExecutionTimeout})");
        }
        public Task<string> Subscribe(string eventKey, Func<DomainEvent, IServiceProvider, Task> handler, string name) =>
            _subscriptionManager.Subscribe(_defaultNamespace, eventKey, handler, name);
        public Task Unsubscribe(string handlerId) => _subscriptionManager.Unsubscribe(handlerId);
    }
}