using System.Security.Claims;
using AnyService.Middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AnyService.Tests.Middlewares
{
    public class DefaultAuthorizationMiddlewareTests
    {
        [Theory]
        [MemberData(nameof(InvokeAsync_HasNullAuthorization_DATA))]
        public async Task InvokeAsync_HasNullAuthorization(WorkContext wc)
        {
            int i = 0, expValue = 15;
            RequestDelegate reqDel = hc =>
            {
                i = expValue;
                return Task.CompletedTask;
            };

            var logger = new Mock<ILogger<DefaultAuthorizationMiddleware>>();
            var mw = new DefaultAuthorizationMiddleware(reqDel, logger.Object);
            await mw.InvokeAsync(null, wc);
            i.ShouldBe(expValue);
        }
        public static IEnumerable<object[]> InvokeAsync_HasNullAuthorization_DATA =>
        new[]
        {
            new object[]{null},
            new object[]{new  WorkContext()},
        };

        [Fact]
        public async Task InvokeAsync_ForbiddenResponse()
        {
            var logger = new Mock<ILogger<DefaultAuthorizationMiddleware>>();
            var mw = new DefaultAuthorizationMiddleware(null, logger.Object);

            var an = new AuthorizeAttribute
            {
                Roles = "role-1",
            };
            var wc = new WorkContext
            {
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EndpointSettings = new EndpointSettings
                    {
                        Route = "/test",
                        GetSettings = new EndpointMethodSettings { Authorization = an }
                    }
                }
            };

            var ctx = new Mock<HttpContext>();
            var req = new Mock<HttpRequest>();
            req.Setup(r => r.Method).Returns("get");

            var res = new Mock<HttpResponse>();
            ctx.SetupGet(h => h.Response).Returns(res.Object);
            ctx.SetupGet(h => h.Request).Returns(req.Object);

            var claims = new[] { new Claim(ClaimTypes.Role, "not-role-1") };

            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            ctx.Setup(r => r.User).Returns(principal);

            await mw.InvokeAsync(ctx.Object, wc);
            res.VerifySet(r => r.StatusCode = StatusCodes.Status403Forbidden, Times.Once);
        }
        [Fact]
        public async Task InvokeAsync_MoveToNext()
        {
            int i = 0, expValue = 15;
            RequestDelegate reqDel = hc =>
            {
                i = expValue;
                return Task.CompletedTask;
            };
            var logger = new Mock<ILogger<DefaultAuthorizationMiddleware>>();
            var mw = new DefaultAuthorizationMiddleware(reqDel, logger.Object);
            var role = "role-1";
            var an = new AuthorizeAttribute
            {
                Roles = role,
            };
            var wc = new WorkContext
            {
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EndpointSettings = new EndpointSettings
                    {
                        Route = "/test",
                        GetSettings = new EndpointMethodSettings { Authorization = an }
                    }
                }
            };

            var ctx = new Mock<HttpContext>();
            var req = new Mock<HttpRequest>();
            req.Setup(r => r.Method).Returns("get");

            var res = new Mock<HttpResponse>();
            ctx.SetupGet(h => h.Response).Returns(res.Object);
            ctx.SetupGet(h => h.Request).Returns(req.Object);

            var claims = new[] { new Claim(ClaimTypes.Role, role) };

            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            ctx.Setup(r => r.User).Returns(principal);

            await mw.InvokeAsync(ctx.Object, wc);
            i.ShouldBe(expValue);
        }
    }
}
