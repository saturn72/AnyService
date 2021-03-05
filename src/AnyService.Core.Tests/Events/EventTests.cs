using AnyService.Events;
using Shouldly;
using System;
using Xunit;

namespace AnyService.Core.Tests.Events
{
    public class EventTests
    {
        [Fact]
        public void Event_InitAllFields()
        {
            var e = new Event();
            e.Id.ShouldNotBeNullOrWhiteSpace();
            e.Id.ShouldNotBeNullOrEmpty();

            e.PublishedOnUtc.ShouldBeGreaterThan(default);
            e.PublishedOnUtc.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        }
    }
}
