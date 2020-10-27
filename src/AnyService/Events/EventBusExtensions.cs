using System;

namespace AnyService.Events
{
    public static class EventBusExtensions
    {
        public static void Publish<T>(this IEventBus eventBus, string eventKey, T data, WorkContext workContext)
        {
            var ded = new DomainEvent
            {
                Data = data,
                PerformedByUserId = workContext.CurrentUserId,
                WorkContext = workContext,
            };
            eventBus.Publish(eventKey, ded);
        }
        public static void PublishUpdated<T>(this IEventBus eventBus, string eventKey, T before, T after, WorkContext workContext)
        {
            var ded = new EntityUpdatedDomainEvent<T>(before, after)
            {
                PerformedByUserId = workContext.CurrentUserId,
                WorkContext = workContext
            };
            eventBus.Publish(eventKey, ded);
        }
        public static void PublishException(this IEventBus eventBus, string eventKey, Exception exception, object data, WorkContext workContext)
        {
            var ded = new DomainExceptionEvent(exception, data, workContext.TraceId)
            {
                PerformedByUserId = workContext.CurrentUserId,
                WorkContext = workContext
            };
            eventBus.Publish(eventKey, ded);
        }
    }
}