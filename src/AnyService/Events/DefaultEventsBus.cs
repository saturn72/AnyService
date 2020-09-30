using AnyService.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public class DefaultEventsBus : IEventBus
    {
        private readonly ILogger<DefaultEventsBus> _logger;
        private readonly IDictionary<string, ICollection<HandlerData>> _handlers;
        public DefaultEventsBus(ILogger<DefaultEventsBus> logger)
        {
            _logger = logger;
            _handlers = new Dictionary<string, ICollection<HandlerData>>();
        }
        public void Publish(string eventKey, DomainEventData eventData)
        {
            _logger.LogInformation(LoggingEvents.EventPublishing, $"Publishing event with key: {eventKey}, data: {eventData.ToJsonString()}");
            if (_handlers.TryGetValue(eventKey, out ICollection<HandlerData> handlerDatas))
            {
                foreach (var h in handlerDatas)
                {
                    _logger.LogInformation(LoggingEvents.EventPublishing, $"Publishing event to handler named {h.Name}");
                    eventData.PublishedOnUtc = DateTime.UtcNow;
                    var t = h.Handler(eventData);
                }
            }
        }
        public string Subscribe(string eventKey, Func<DomainEventData, Task> handler, string name)
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
            public Func<DomainEventData, Task> Handler { get; set; }
        }
        #endregion
    }
}