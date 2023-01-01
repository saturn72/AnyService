using AnyService.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AnyService.Tests.Middlewares
{
    public class OnMissingUserIdWorkContextMiddlewareHandlersTests
    {
        [Fact]
        public async Task DefaultOnMissingUserIdHandler_Test()
        {
            var statusCode = 0;
            var hc = new Mock<HttpContext>();
            hc.SetupSet(_ => _.Response.StatusCode = It.Is<int>(c => c == StatusCodes.Status401Unauthorized)).Callback<int>(c => statusCode = c);

            var l = new Mock<ILogger>();
            var res = await OnMissingUserIdWorkContextMiddlewareHandlers.DefaultOnMissingUserIdHandler(hc.Object, null, l.Object);
            res.ShouldBeFalse();
            statusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        }

        [Theory]
        //[InlineData("/no-auth-required", true, 0)]
        [InlineData("/no-auth-required-false", false, 401)]
        //[InlineData("/auth-required", false, 401)]
        public async Task PermittedPathsOnMissingUserIdHandler_Test(string path, bool isPermitted, int expStatusCode)
        {
            var statusCode = 0;
            var hc = new Mock<HttpContext>();
            hc.SetupGet(_ => _.Request.Path).Returns(new PathString(path));
            hc.SetupSet(_ => _.Response.StatusCode = It.IsAny<int>()).Callback<int>(c => statusCode = c);

            var l = new Mock<ILogger>();
            var handler = OnMissingUserIdWorkContextMiddlewareHandlers.PermittedPathsOnMissingUserIdHandler(new[] { new PathString("/no-auth-required") });
            var res = await handler(hc.Object, null, l.Object);
            res.ShouldBe(isPermitted);
            statusCode.ShouldBe(expStatusCode);
        }
    }
}
