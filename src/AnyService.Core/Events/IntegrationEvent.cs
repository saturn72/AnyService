using System;

namespace AnyService.Events
{
    public class IntegrationEvent
    {
        public IntegrationEvent()
        {
            Id = Guid.NewGuid().ToString();
            PublishedOnUtc = DateTime.UtcNow;
        }
        public string Id { get; }
        public DateTime PublishedOnUtc { get; }
        public object Data { get; set; }
    }
}
