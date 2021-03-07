using AnyService.Events;
using Shouldly;
using System;
using Xunit;

namespace AnyService.Core.Tests.Events
{
    public class IntegrationEventTests
    {
        [Fact]
        public void Event_InitAllFields()
        {
            var route = "test";
            var e = new IntegrationEvent(route);

            e.Route.ShouldBe(route);

            e.Id.ShouldNotBeNullOrWhiteSpace();
            e.Id.ShouldNotBeNullOrEmpty();

            e.PublishedOnUtc.ShouldBeGreaterThan(default);
            e.PublishedOnUtc.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        }
    }
}
