using AnyService.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public class DefaultEventsBus : IEventBus
    {
        private const int TaskExecutionTimeout = 60000;

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DefaultEventsBus> _logger;
        private readonly IDictionary<string, ICollection<HandlerData>> _handlers;
        public DefaultEventsBus(
            IServiceProvider serviceProvider,
            ILogger<DefaultEventsBus> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _handlers = new Dictionary<string, ICollection<HandlerData>>();
        }
        public void Publish(string eventKey, Event @event)
        {
            _logger.LogInformation(LoggingEvents.EventPublishing, $"Publishing event with key: {eventKey}");
            if (_handlers.TryGetValue(eventKey, out ICollection<HandlerData> handlerDatas))
            {
                var tasks = new List<Task>();
                using var scope = _serviceProvider.CreateScope();
                foreach (var h in handlerDatas)
                {
                    _logger.LogInformation(LoggingEvents.EventPublishing, $"Publishing event to handler named {h.Name}");
                    tasks.Add(h.Handler(@event, scope.ServiceProvider));
                }
                if (!Task.WaitAll(tasks.ToArray(), TaskExecutionTimeout))
                    _logger.LogDebug($"Not all tasks finished execution within given timout ({TaskExecutionTimeout})");
            }
        }
        public string Subscribe(string eventKey, Func<Event, IServiceProvider, Task> handler, string name)
        {
            var handlerId = Convert
                .ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("=", "")
                .Replace("+", "");

            var handlerData = new HandlerData
            {
                HandlerId = handlerId,
                Handler = handler,
                Name = name,
            };

            if (_handlers.ContainsKey(eventKey))
                _handlers[eventKey].Add(handlerData);
            else
                _handlers[eventKey] = new List<HandlerData> { handlerData };

            return handlerId;
        }


        public void Unsubscribe(string handlerId)
        {
            var handlerDatas = _handlers.Values.FirstOrDefault(hd => hd.Any(x => x.HandlerId.Equals(handlerId, StringComparison.InvariantCultureIgnoreCase)));
            if (handlerDatas != null)
                (handlerDatas as List<HandlerData>).RemoveAll(x => x.HandlerId.Equals(handlerId, StringComparison.InvariantCultureIgnoreCase));
        }

        #region nested classes
        private class HandlerData
        {
            public string HandlerId { get; set; }
            public string Name { get; set; }
            public Func<Event, IServiceProvider, Task> Handler { get; set; }
        }
        #endregion
    }
}