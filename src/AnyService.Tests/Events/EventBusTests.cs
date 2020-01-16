using System;
using System.Threading;
using AnyService.Events;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Events
{
    public class EventBusTests
    {
        [Fact]
        public void PublishWithoutSubscription()
        {
            var handleCounter = 0;
            var ek = "event-key";
            var ed = new DomainEventData
            {
                Data = "thjis is data"
            };
            var handler = new Action<DomainEventData>(d => handleCounter++);

            var eb = new DomainEventsBus();
            eb.Publish(ek, ed);
            handleCounter.ShouldBe(0);
            ed.PublishedOnUtc.ShouldBeGreaterThan(default(DateTime));
        }
        [Fact]
        public void PublishWithSubscription_ThenUnsubscribe()
        {
            var handleCounter = 0;
            var ek = "event-key";
            var ed = new DomainEventData
            {
                Data = "thjis is data"
            };
            var handler = new Action<DomainEventData>(d => handleCounter++);

            var eb = new DomainEventsBus();
            var handlerId = eb.Subscribe(ek, handler);
            eb.Publish(ek, ed);
            Thread.Sleep(50);
            handleCounter.ShouldBe(1);
            ed.PublishedOnUtc.ShouldBeGreaterThan(default(DateTime));

            ed.PublishedOnUtc = default(DateTime);
            eb.Unsubscribe(handlerId);
            eb.Publish(ek, ed);
            Thread.Sleep(50);
            handleCounter.ShouldBe(1);
            ed.PublishedOnUtc.ShouldBeGreaterThan(default(DateTime));
        }
    }
}