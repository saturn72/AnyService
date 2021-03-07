using System;

namespace AnyService.Events
{
    public class IntegrationEvent
    {
        public IntegrationEvent(string route)
        {
            Route = route;
            Id = Guid.NewGuid().ToString();
            PublishedOnUtc = DateTime.UtcNow;
        }
        public string Route { get; }
        public string Id { get; }
        public DateTime PublishedOnUtc { get; }
        public object Data { get; set; }
    }
}
