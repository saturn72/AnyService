using System;
using System.Threading;
using System.Threading.Tasks;
using AnyService.Events;
using Microsoft.Extensions.Logging;
using Moq;
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
            var ed = new DomainEvent
            {
                Data = "this is data"
            };

            var l = new Mock<ILogger<DefaultEventsBus>>();
            var eb = new DefaultEventsBus(l.Object);

            eb.Publish(ek, ed);
            ed.PublishedOnUtc.ShouldBe(default);
        }
        [Fact]
        public void PublishWithSubscription_ThenUnsubscribe()
        {
            var handleCounter = 0;
            var ek = "event-key";
            var ed = new DomainEvent
            {
                Data = "thjis is data"
            };
            var handler = new Func<DomainEvent, Task>(d =>
            {
                handleCounter++;
                return Task.CompletedTask;
            });

            var l = new Mock<ILogger<DefaultEventsBus>>();
            var eb = new DefaultEventsBus(l.Object);
            var handlerId = eb.Subscribe(ek, handler, "name");
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