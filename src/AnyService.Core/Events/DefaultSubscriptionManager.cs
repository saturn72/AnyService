using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public class DefaultSubscriptionManager<TEvent> : ISubscriptionManager<TEvent> where TEvent : class
    {
        private readonly ILogger<DefaultSubscriptionManager<TEvent>> _logger;
        private readonly IDictionary<string, IDictionary<string, ICollection<HandlerData<TEvent>>>> _namespaceHandlers;
        private object lockObj = new object();

        public DefaultSubscriptionManager(
            ILogger<DefaultSubscriptionManager<TEvent>> logger)
        {
            _logger = logger;
            _namespaceHandlers = new Dictionary<string, IDictionary<string, ICollection<HandlerData<TEvent>>>>();
        }
        public Task<string> Subscribe(string @namespace, string eventKey, Func<TEvent, IServiceProvider, Task> handler, string alias)
        {
            _logger.LogDebug("Subscribing event handler for {EventKey} with {Name}", eventKey, alias);
            var handlerId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var handlerData = new HandlerData<TEvent>(@namespace, eventKey)
            {
                HandlerId = handlerId,
                Handler = handler,
                Alias = alias,
            };
            lock (lockObj)
            {
                if (!_namespaceHandlers.TryGetValue(@namespace, out IDictionary<string, ICollection<HandlerData<TEvent>>> nsh))
                {
                    nsh = new Dictionary<string, ICollection<HandlerData<TEvent>>>();
                    _namespaceHandlers[@namespace] = nsh;
                }

                if (!nsh.TryGetValue(eventKey, out ICollection<HandlerData<TEvent>> handlers))
                {
                    handlers = new List<HandlerData<TEvent>>();
                    nsh[eventKey] = handlers;
                }
                handlers.Add(handlerData);
            }
            return Task.FromResult(handlerId);
        }
        public Task Unsubscribe(string handlerId)
        {
            var allEventKeys = _namespaceHandlers.SelectMany(x => x.Value).SelectMany(y => y.Value);
            var hd = allEventKeys.FirstOrDefault(h => h.HandlerId == handlerId);
            if (hd != null)
                _namespaceHandlers[hd.Namespace][hd.EventKey].Remove(hd);

            return Task.CompletedTask;
        }
        public Task<IEnumerable<HandlerData<TEvent>>> GetHandlers(string @namespace, string eventKey)
        {
            ICollection<HandlerData<TEvent>> handlers = null;
            _ = _namespaceHandlers.TryGetValue(@namespace, out IDictionary<string, ICollection<HandlerData<TEvent>>> nsh) &&
                nsh.TryGetValue(eventKey, out handlers);

            return Task.FromResult(handlers?.AsEnumerable());

        }
        public Task<IEnumerable<HandlerData<TEvent>>> GetAllHandlers()
        {
            var allHandlers = _namespaceHandlers?.SelectMany(x => x.Value).SelectMany(y => y.Value);
            return Task.FromResult(allHandlers);
        }
        public void Clear() => _namespaceHandlers.Clear();

        public Task<HandlerData<TEvent>> GetByHandlerId(string handlerId)
        {
            var handler = _namespaceHandlers?
                .SelectMany(x => x.Value)?
                .SelectMany(y => y.Value)?
                .FirstOrDefault(h => h.HandlerId == handlerId);
            return Task.FromResult(handler);
        }
    }
}