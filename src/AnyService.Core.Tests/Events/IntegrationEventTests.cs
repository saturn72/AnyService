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
            var ns = "test";
            var ek = "k";
            var e = new IntegrationEvent(ns, ek);

            e.Exchange.ShouldBe(ns);

            e.Id.ShouldNotBeNullOrWhiteSpace();
            e.Id.ShouldNotBeNullOrEmpty();

            e.PublishedOnUtc.ShouldBeGreaterThan(default);
            e.PublishedOnUtc.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        }
        [Fact]
        public void Clone_WithDifferentNamespaceAndEventKey()
        {
            string ns = "test",
                ek = "k",
                newNs = "new-ns",
                newEk = "new-ek";
            var e = new IntegrationEvent(ns, ek);
            var k = e.Clone(newNs, newEk);
            k.GetHashCode().ShouldNotBe(e.GetHashCode());
            k.Exchange.ShouldBe(newNs);
            k.RoutingKey.ShouldBe(newEk);
            k.ReferenceId.ShouldBe(e.ReferenceId);
            k.Id.ShouldNotBe(e.Id);

            k.Data.ShouldBe(e.Data);
            k.PublishedOnUtc.ShouldBe(e.PublishedOnUtc);
        }
        [Fact]
        public void Clone()
        {
            string ns = "test",
                ek = "k";
            var e = new IntegrationEvent(ns, ek);
            var k = e.Clone();
            k.GetHashCode().ShouldNotBe(e.GetHashCode());
            k.Exchange.ShouldBe(e.Exchange);
            k.RoutingKey.ShouldBe(e.RoutingKey);
            k.Id.ShouldNotBe(e.Id);

            k.Data.ShouldBe(e.Data);
            k.PublishedOnUtc.ShouldBe(e.PublishedOnUtc);
        }
    }
}