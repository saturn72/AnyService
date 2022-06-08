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

            var f = new Func<IntegrationEvent, IServiceProvider, Task>((e, s) => Task.Run(() => i = expValue));
            var l = new Mock<ILogger<DefaultSubscriptionManager<IntegrationEvent>>>();
            var sm = new DefaultSubscriptionManager<IntegrationEvent>(l.Object);

            var handlers = await sm.GetHandlers("default", ek);
            handlers.ShouldBeNull();
            var hId = await sm.Subscribe("default", ek, f, "test");
            hId.ShouldNotBeNullOrEmpty();

            var h = await sm.GetHandlerById(new[] { hId });
            h.ShouldNotBeEmpty();

            await sm.Unsubscribe(hId);
            h = await sm.GetHandlerById(new[] { hId });
            h.ShouldBeEmpty();

            hId = await sm.Subscribe("default", ek, f, "test");
            hId.ShouldNotBeNullOrEmpty();

            var hds = await sm.GetHandlers("default", ek);
            hds.Count().ShouldBe(1);
            await hds.First().Handler(null, null);
            i.ShouldBe(expValue);

            await sm.Unsubscribe(hId);
            hds = await sm.GetHandlers("default", ek);
            hds.Count().ShouldBe(0);
        }
    }
}