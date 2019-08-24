using System;

namespace AnyService.Events
{
    public sealed class EventData
    {
        public string CurrentUserId { get; set; }
        public DateTime PublishedOnUtc { get; set; }
        public object Data { get; set; }
    }
}