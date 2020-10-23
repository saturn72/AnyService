using System;

namespace AnyService.Events
{
    public sealed class DomainEventData
    {
        public string PerformedByUserId { get; set; }
        public string PerformedUsingClientId { get; set; }
        public DateTime PublishedOnUtc { get; set; }
        public WorkContext WorkContext { get; set; }
        public object Data { get; set; }
    }
}