
namespace AnyService.Tests
{
    public class ConstsTests
    {
        [Fact]
        public void AllConsts()
        {
            Consts.ReservedPrefix.ShouldBe("__");
            Consts.MultipartSuffix.ShouldBe("__multipart");
            Consts.StreamSuffix.ShouldBe("__stream");
            Consts.ControllerAuthzPolicy.ShouldBe("controller-policy");
        }
    }
}