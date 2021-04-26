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

            e.Namespace.ShouldBe(ns);

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
            k.Namespace.ShouldBe(newNs);
            k.EventKey.ShouldBe(newEk);
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
            k.Namespace.ShouldBe(e.Namespace);
            k.EventKey.ShouldBe(e.EventKey);
            k.Id.ShouldNotBe(e.Id);

            k.Data.ShouldBe(e.Data);
            k.PublishedOnUtc.ShouldBe(e.PublishedOnUtc);
        }
    }
}