﻿using System;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public static class EventBusExtensions
    {
        public static Task Publish<TData>(this IDomainEventBus eventBus, string eventKey, TData data)
        {
            return eventBus.Publish(eventKey, new DomainEvent
            {
                Data = data,
            });
        }
        public static void Publish(this IDomainEventBus eventBus, string eventKey, IEntity data, WorkContext workContext)
        {
            var ded = new DomainEvent
            {
                Data = data,
                PerformedByUserId = workContext.CurrentUserId,
                WorkContext = workContext,
            };
            eventBus.Publish(eventKey, ded);
        }
        public static void PublishUpdated(this IDomainEventBus eventBus, string eventKey, IEntity before, IEntity after, WorkContext workContext)
        {
            var ded = new EntityUpdatedDomainEvent(before, after)
            {
                PerformedByUserId = workContext.CurrentUserId,
                WorkContext = workContext
            };
            eventBus.Publish(eventKey, ded);
        }
        public static void PublishException(this IDomainEventBus eventBus, string eventKey, Exception exception, object data, WorkContext workContext)
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