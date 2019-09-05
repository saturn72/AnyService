using AnyService.Services;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Services
{
    public class EventKeysTests
    {
        [Fact]
        public void ReturnAllKeys()
        {
            var ekr = new EventKeyRecord("i_create", "i_read", "i_update", "i_delete");
            var iTcr = new TypeConfigRecord(typeof(int), "some-route-prefix", ekr);
            var tcr = new[] { iTcr };
            var ek = new EventKeys(tcr);

            var i = ek[iTcr.Type];
            i.Create.ShouldBe(ekr.Create);
            i.Read.ShouldBe(ekr.Read);
            i.Update.ShouldBe(ekr.Update);
            i.Delete.ShouldBe(ekr.Delete);
        }
    }
}