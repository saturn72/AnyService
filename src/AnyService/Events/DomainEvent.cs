using System;

namespace AnyService.Events
{
    public class DomainEvent
    {
        public string PerformedByUserId { get; set; }
        public DateTime PublishedOnUtc { get; set; }
        public WorkContext WorkContext { get; set; }
        public object Data { get; set; }
    }
}