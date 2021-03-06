using System;

namespace AnyService.Events
{
    public class DomainEvent
    {
        public DomainEvent()
        {
            Id = Guid.NewGuid().ToString();
            PublishedOnUtc = DateTime.UtcNow;
        }
        public string Id { get; }
        public DateTime PublishedOnUtc { get; }
        public object Data { get; set; }
        public string PerformedByUserId { get; set; }
        public WorkContext WorkContext { get; set; }
    }
}