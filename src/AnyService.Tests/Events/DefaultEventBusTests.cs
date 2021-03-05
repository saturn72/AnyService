using System;
using System.Threading;
using System.Threading.Tasks;
using AnyService.Events;
using Microsoft.Extensions.DependencyInjection;
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

            var sp = MockServiceProvider();
            var l = new Mock<ILogger<DefaultEventsBus>>();
            var eb = new DefaultEventsBus(sp.Object, l.Object);

            eb.Publish(ek, ed);
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
            var handler = new Func<Event, IServiceProvider, Task>((evt, services) =>
            {
                handleCounter++;
                return Task.CompletedTask;
            });
            var sp = MockServiceProvider();
            var l = new Mock<ILogger<DefaultEventsBus>>();
            var eb = new DefaultEventsBus(sp.Object, l.Object);
            var handlerId = eb.Subscribe(ek, handler, "name");
            eb.Publish(ek, ed);
            Thread.Sleep(50);
            handleCounter.ShouldBe(1);
            ed.PublishedOnUtc.ShouldBeGreaterThan(default(DateTime));

            eb.Unsubscribe(handlerId);
            eb.Publish(ek, ed);
            Thread.Sleep(50);
            handleCounter.ShouldBe(1);
         }
        private Mock<IServiceProvider> MockServiceProvider()
        {
            var sp = new Mock<IServiceProvider>();
            var serviceScope = new Mock<IServiceScope>();
            serviceScope.Setup(x => x.ServiceProvider).Returns(sp.Object);

            var serviceScopeFactory = new Mock<IServiceScopeFactory>();
            serviceScopeFactory
                .Setup(x => x.CreateScope())
                .Returns(serviceScope.Object);

            sp.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
                .Returns(serviceScopeFactory.Object);

            return sp;
        }
    }
}