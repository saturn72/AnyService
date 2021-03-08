using AnyService.Events;
using Shouldly;
using System.Threading;
using Xunit;

namespace AnyService.Core.Tests.Events
{
    public class IntegrationEventExtensionsTests
    {
        [Fact]
        public void Expired_Returns_True()
        {
            var e = new IntegrationEvent("route");
            e.Expiration = 1;
            Thread.Sleep(1100);
            IntegrationEventExtensions.Expired(e).ShouldBeTrue();
        }
        [Fact]
        public void Expired_Returns_False_OnUnlimitedEvent()
        {
            var e = new IntegrationEvent("route");
            IntegrationEventExtensions.Expired(e).ShouldBeFalse();

        }
        [Fact]
        public void Expired_Returns_False_ExpirationEvent()
        {
            var e = new IntegrationEvent("route");
            e.Expiration = 10;
            Thread.Sleep(1);
            IntegrationEventExtensions.Expired(e).ShouldBeFalse();
        }
    }
}