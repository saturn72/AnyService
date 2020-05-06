using System;
using System.Threading;
using System.Threading.Tasks;
using AnyService.Events;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Events
{
    public class DefaultEventBusTests
    {
        [Fact]
        public void PublishWithoutSubscription()
        {
            var ek = "event-key";
            var ed = new DomainEventData
            {
                Data = "thjis is data"
            };

            var eb = new DefaultEventsBus();

            eb.Publish(ek, ed);
            ed.PublishedOnUtc.ShouldBe(default(DateTime));
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
            var handler = new Func<DomainEventData, Task>(d =>
            {
                handleCounter++;
                return Task.CompletedTask;
            });

            var eb = new DefaultEventsBus();
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
            ed.PublishedOnUtc.ShouldBe(default(DateTime));
        }
    }
}