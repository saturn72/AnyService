using System;
using System.Linq;
using System.Threading.Tasks;
using AnyService.Events;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Events
{
    public class DefaultSubscriptionManagerTests
    {
        [Fact]
        public async Task Subscribe_Publish_Unsubscribe()
        {
            int i = 100,
                expValue = 777;
            var ek = "e-key";

            var f = new Func<DomainEvent, IServiceProvider, Task>((e, s) => Task.Run(() => i = expValue));
            var l = new Mock<ILogger<DefaultSubscriptionManager<DomainEvent>>>();
            var sm = new DefaultSubscriptionManager<DomainEvent>(l.Object);

            var h = await sm.GetHandlers(ek);
            h.ShouldBeNull();
            var hId = await sm.Subscribe(ek, f, "test");
            hId.ShouldNotBeNullOrEmpty();

            var hds = await sm.GetHandlers(ek);
            hds.Count().ShouldBe(1);
            await hds.First().Handler(null, null);
            i.ShouldBe(expValue);
            
            await sm.Unsubscribe(hId);
            hds = await sm.GetHandlers(ek);
            hds.Count().ShouldBe(0);
        }
    }
}