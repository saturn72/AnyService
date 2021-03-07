using AnyService.Events;
using Shouldly;
using Xunit;

namespace AnyService.Core.Tests.Events
{
    public class HandlerDataTests
    {
        [Fact]
        public void InitAllFields()
        {
            var ns = "ns";
            var ek = "ek";
            var hd = new HandlerData<string>(ns, ek);

            hd.Namespace.ShouldBe(ns);
            hd.EventKey.ShouldBe(ek);
        }
    }
}
