using System;

namespace AnyService.Events
{
    public static class IntegrationEventExtensions
    {
        public static bool Expired(this IntegrationEvent @event) => @event.Expiration !=null && DateTime.UtcNow <= @event.PublishedOnUtc.AddSeconds(@event.Expiration.Value);
    }
}
