using Xunit;
using Shouldly;

namespace AnyService.Tests
{
    public class ConstsTests
    {
        [Fact]
        public void AllConsts()
        {
            Consts.AnyServiceControllerName.ShouldBe("_anyservice");
            Consts.MultipartSuffix.ShouldBe("_multipart");
        }
    }
}