using System;

namespace AnyService.Events
{
    public class IntegrationEvent
    {
        public IntegrationEvent(string @namespace, string eventKey)
        {
            Namespace = @namespace;
            EventKey = eventKey;
            ReferenceId = Guid.NewGuid().ToString();
            Id = Guid.NewGuid().ToString();
            PublishedOnUtc = DateTime.UtcNow;
        }
        public string Namespace { get; }
        public string EventKey { get; }
        public string ReferenceId { get; private set; }
        public string Id { get; }
        public DateTime PublishedOnUtc { get; private set; }
        public int? Expiration { get; set; }
        public object Data { get; set; }

        public IntegrationEvent Clone(string newNamespace, string newEventKey)
        {
            return new IntegrationEvent(newNamespace, newEventKey)
            {
                ReferenceId = ReferenceId,
                PublishedOnUtc = PublishedOnUtc,
                Expiration = Expiration,
                Data = Data
            };
        }
        public IntegrationEvent Clone()
        {
            return new IntegrationEvent(Namespace, EventKey)
            {
                PublishedOnUtc = PublishedOnUtc,
                Expiration = Expiration,
                Data = Data
            };
        }
    }
}
