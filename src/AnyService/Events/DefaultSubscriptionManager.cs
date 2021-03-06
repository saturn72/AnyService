using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public class DefaultSubscriptionManager<TEvent> : ISubscriptionManager<TEvent> where TEvent:class
    {
        private readonly ILogger<DefaultSubscriptionManager<TEvent>> _logger;
        private readonly IDictionary<string, ICollection<HandlerData<TEvent>>> _handlers;

        public DefaultSubscriptionManager(
            ILogger<DefaultSubscriptionManager<TEvent>> logger)
        {
            _logger = logger;
            _handlers = new Dictionary<string, ICollection<HandlerData<TEvent>>>();
        }
        public Task<string> Subscribe(string eventKey, Func<TEvent, IServiceProvider, Task> handler, string name)
        {
            _logger.LogDebug("Subscribing event handler for {EventKey} with {Name}", eventKey, name);
            var handlerId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var handlerData = new HandlerData<TEvent>
            {
                HandlerId = handlerId,
                Handler = handler,
                Name = name,
            };

            if (_handlers.ContainsKey(eventKey))
                _handlers[eventKey].Add(handlerData);
            else
                _handlers[eventKey] = new List<HandlerData<TEvent>> { handlerData };

            return Task.FromResult(handlerId);
        }
        public Task Unsubscribe(string handlerId)
        {
            var handlerDatas = _handlers.Values.FirstOrDefault(hd => hd.Any(x => x.HandlerId.Equals(handlerId, StringComparison.InvariantCultureIgnoreCase)));
            if (handlerDatas != null)
                (handlerDatas as List<HandlerData<TEvent>>).RemoveAll(x => x.HandlerId.Equals(handlerId, StringComparison.InvariantCultureIgnoreCase));

            return Task.CompletedTask;
        }
        public Task<IEnumerable<HandlerData<TEvent>>> GetHandlers(string eventKey)
        {
            _handlers.TryGetValue(eventKey, out ICollection<HandlerData<TEvent>> handlerDatas);
            return Task.FromResult(handlerDatas?.AsEnumerable());
        }
        public Task<IEnumerable<HandlerData<TEvent>>> GetAllHandlers()
        {
            var res = _handlers.SelectMany(x => x.Value);
            return Task.FromResult(res?.AsEnumerable());
        }
        public void Clear() => _handlers.Clear();
    }
}