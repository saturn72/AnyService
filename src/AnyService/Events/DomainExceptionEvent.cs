using System;

namespace AnyService.Events
{
    public class DomainExceptionEvent : DomainEvent
    {
        public DomainExceptionEvent(Exception exception, object data, string traceId)
        {
            Data = new DomainExceptionEventData
            {
                Exception = exception,
                TraceId = traceId,
                Data = data,
            };
        }

        public class DomainExceptionEventData
        {
            public Exception Exception { get; set; }
            public string TraceId { get; set; }
            public object Data { get; set; }
        }
    }
}