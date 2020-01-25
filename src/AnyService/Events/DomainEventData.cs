using System;

namespace AnyService.Events
{
    public sealed class DomainEventData
    {
        public string PerformedByUserId { get; set; }
        public DateTime PublishedOnUtc { get; set; }
        public object Data { get; set; }
    }
}