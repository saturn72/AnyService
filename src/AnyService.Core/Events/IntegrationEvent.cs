using System;

namespace AnyService.Events
{
    public class IntegrationEvent
    {
        public IntegrationEvent(string exchange, string routingKey, int? expiration = null)
        {
            Exchange = exchange;
            RoutingKey = routingKey;
            ReferenceId = Guid.NewGuid().ToString();
            Id = Guid.NewGuid().ToString();
            PublishedOnUtc = DateTime.UtcNow;
            Expiration = expiration;
        }
        public string Exchange { get; }
        public string RoutingKey { get; }
        public string ReferenceId { get; private set; }
        public string Id { get; }
        public DateTime PublishedOnUtc { get; private set; }
        /// <summary>
        /// Gets or sets the expiration of the message in milisecs
        /// </summary>
        public int? Expiration { get; set; }
        public object Data { get; set; }

        public IntegrationEvent Clone(string newExchange, string newRoutingKey)
        {
            return new IntegrationEvent(newExchange, newRoutingKey)
            {
                ReferenceId = ReferenceId,
                PublishedOnUtc = PublishedOnUtc,
                Expiration = Expiration,
                Data = Data
            };
        }
        public IntegrationEvent Clone()
        {
            return new IntegrationEvent(Exchange, RoutingKey)
            {
                PublishedOnUtc = PublishedOnUtc,
                Expiration = Expiration,
                Data = Data,
            };
        }
    }
}
