using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public class DefaultSubscriptionManager : ISubscriptionManager
    {
        private readonly ILogger<DefaultSubscriptionManager> _logger;
        private readonly IDictionary<string, ICollection<HandlerData>> _handlers;

        public DefaultSubscriptionManager(
            ILogger<DefaultSubscriptionManager> logger)
        {
            _logger = logger;
            _handlers = new Dictionary<string, ICollection<HandlerData>>();
        }
        public Task<string> Subscribe(string eventKey, Func<Event, IServiceProvider, Task> handler, string name)
        {
            _logger.LogDebug("Subscribing event handler for {EventKey} with {Name}", eventKey, name);
            var handlerId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
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

            return Task.FromResult(handlerId);
        }
        public Task Unsubscribe(string handlerId)
        {
            var handlerDatas = _handlers.Values.FirstOrDefault(hd => hd.Any(x => x.HandlerId.Equals(handlerId, StringComparison.InvariantCultureIgnoreCase)));
            if (handlerDatas != null)
                (handlerDatas as List<HandlerData>).RemoveAll(x => x.HandlerId.Equals(handlerId, StringComparison.InvariantCultureIgnoreCase));

            return Task.CompletedTask;
        }
        public Task<IEnumerable<HandlerData>> GetHandlers(string eventKey)
        {
            _handlers.TryGetValue(eventKey, out ICollection<HandlerData> handlerDatas);
            return Task.FromResult(handlerDatas?.AsEnumerable());
        }
        public Task<IEnumerable<HandlerData>> GetAllHandlers()
        {
            var res = _handlers.SelectMany(x => x.Value);
            return Task.FromResult(res?.AsEnumerable());
        }
        public void Clear() => _handlers.Clear();
    }
}