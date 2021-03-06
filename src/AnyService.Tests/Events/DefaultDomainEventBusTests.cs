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
    public class DefaultDomainEventBusTests
    {
        [Fact]
        public async Task PublishWithoutSubscription()
        {
            var ek = "event-key";
            var ed = new DomainEvent
            {
                Data = "this is data"
            };

            var sm = new Mock<ISubscriptionManager<DomainEvent>>();
            var sp = MockServiceProvider();
            var l = new Mock<ILogger<DefaultDomainEventsBus>>();
            var eb = new DefaultDomainEventsBus(sm.Object, sp.Object, l.Object);

            await eb.Publish(ek, ed);
        }
        [Fact]
        public async Task PublishWithSubscription_ThenUnsubscribe()
        {
            var handleCounter = 0;
            var ek = "event-key";
            var ed = new DomainEvent
            {
                Data = "thjis is data"
            };
            var handler = new Func<DomainEvent, IServiceProvider, Task>((evt, services) =>
            {
                handleCounter++;
                return Task.CompletedTask;
            });

            var sm = new Mock<ISubscriptionManager<DomainEvent>>();
            sm.Setup(s => s.GetHandlers(It.Is<string>(str => str == ek)))
                .ReturnsAsync(new[]
                {
                    new HandlerData<DomainEvent>
                    {
                        Handler = handler
                    }
                });
            var sp = MockServiceProvider();
            var l = new Mock<ILogger<DefaultDomainEventsBus>>();
            var eb = new DefaultDomainEventsBus(sm.Object, sp.Object, l.Object);
            var handlerId = await eb.Subscribe(ek, handler, "name");
            await eb.Publish(ek, ed);
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