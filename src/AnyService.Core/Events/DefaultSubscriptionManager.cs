using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public class DefaultSubscriptionManager<TEvent> : ISubscriptionManager<TEvent> where TEvent : class
    {
        private const string EMPTY_ROUTINGKEY_REPLACMENT_VALUE = "-**-";
        private readonly ILogger<DefaultSubscriptionManager<TEvent>> _logger;
        private readonly IDictionary<string, IDictionary<string, ICollection<HandlerData<TEvent>>>> _exchangeHandlers;

        private readonly object lockObj = new object();

        public DefaultSubscriptionManager(
            ILogger<DefaultSubscriptionManager<TEvent>> logger)
        {
            _logger = logger;
            _exchangeHandlers = new Dictionary<string, IDictionary<string, ICollection<HandlerData<TEvent>>>>();
        }
        public Task<string> Subscribe(string exchange, string routingKey, Func<TEvent, IServiceProvider, Task> handler, string alias)
        {
            _logger.LogDebug("Subscribing event handler for {EventKey} with {Name}", routingKey, alias);
            var handlerId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var handlerData = new HandlerData<TEvent>(exchange, routingKey)
            {
                HandlerId = handlerId,
                Handler = handler,
                Alias = alias,
            };
            if (!routingKey.HasValue())
                routingKey = EMPTY_ROUTINGKEY_REPLACMENT_VALUE;
            lock (lockObj)
            {
                //has exchange and routing key
                if (!_exchangeHandlers.TryGetValue(exchange, out IDictionary<string, ICollection<HandlerData<TEvent>>> nsh))
                {
                    nsh = new Dictionary<string, ICollection<HandlerData<TEvent>>>();
                    _exchangeHandlers[exchange] = nsh;
                }

                if (!nsh.TryGetValue(routingKey, out ICollection<HandlerData<TEvent>> handlers))
                {
                    handlers = new List<HandlerData<TEvent>>();
                    nsh[routingKey] = handlers;
                }
                handlers.Add(handlerData);
            }
            return Task.FromResult(handlerId);
        }
        public Task Unsubscribe(string handlerId)
        {
            var allEventKeys = _exchangeHandlers.SelectMany(x => x.Value).SelectMany(y => y.Value);
            var hd = allEventKeys.FirstOrDefault(h => h.HandlerId == handlerId);
            if (hd != null)
                _exchangeHandlers[hd.Namespace][hd.EventKey].Remove(hd);

            return Task.CompletedTask;
        }
        public Task<IEnumerable<HandlerData<TEvent>>> GetHandlers(string exchange, string routingKey)
        {
            if (!routingKey.HasValue())
                routingKey = EMPTY_ROUTINGKEY_REPLACMENT_VALUE;

            ICollection<HandlerData<TEvent>> handlers = null;
            _ = _exchangeHandlers.TryGetValue(exchange, out IDictionary<string, ICollection<HandlerData<TEvent>>> nsh) &&
                nsh.TryGetValue(routingKey, out handlers);

            return Task.FromResult(handlers?.AsEnumerable());

        }
        public Task<IEnumerable<HandlerData<TEvent>>> GetAllHandlers()
        {
            var allHandlers = _exchangeHandlers?.SelectMany(x => x.Value).SelectMany(y => y.Value);
            return Task.FromResult(allHandlers);
        }
        public void Clear() => _exchangeHandlers.Clear();

        public Task<IEnumerable<HandlerData<TEvent>>> GetHandlerById(IEnumerable<string> handlerIds)
        {
            var handlers = _exchangeHandlers?
                .SelectMany(x => x.Value)?
                .SelectMany(y => y.Value)?
                .Where(h => handlerIds.Contains(h.HandlerId));

            return Task.FromResult(handlers);
        }
    }
}