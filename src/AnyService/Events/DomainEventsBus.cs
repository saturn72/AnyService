using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public class DomainEventsBus : IDomainEventsBus
    {
        private static IDictionary<string, ICollection<HandlerData>> _handlers = new Dictionary<string, ICollection<HandlerData>>();
        public void Publish(string eventKey, DomainEventData eventData)
        {
            if (_handlers.TryGetValue(eventKey, out ICollection<HandlerData> handlerDatas))
                foreach (var h in handlerDatas)
                {
                    eventData.PublishedOnUtc = DateTime.UtcNow;
                    Task.Run(() => h.Handler(eventData));
                }
        }

        public string Subscribe(string eventKey, Action<DomainEventData> handler)
        {
            var handlerId = Convert
                .ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("=", "")
                .Replace("+", "");

            var handlerData = new HandlerData
            {
                HandlerId = handlerId,
                Handler = handler
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
            public Action<DomainEventData> Handler { get; set; }
        }
        #endregion
    }
}