using AnyService.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Tests.Middlewares
{
    public class WorkContextMiddlewareTests
    {
        [Fact]
        public async Task UserIdIsNull_ReturnUnauthroized()
        {
            var logger = new Mock<ILogger<WorkContextMiddleware>>();
            var ecr = new EntityConfigRecord
            {
                ControllerSettings = new ControllerSettings
                {
                    Route = "/some-resource",
                },
                Type = typeof(string),
            };
            var entityConfigRecords = new[] { ecr };
            var wcm = new WorkContextMiddleware(null, logger.Object, entityConfigRecords);

            var user = new Mock<ClaimsPrincipal>();
            user.Setup(u => u.Claims).Returns(new Claim[] { });
            var hc = new Mock<HttpContext>();
            var hr = new Mock<HttpResponse>();
            hc.Setup(h => h.User).Returns(user.Object);
            hc.Setup(h => h.Response).Returns(hr.Object);
            await wcm.InvokeAsync(hc.Object, null);
            hr.VerifySet(r => r.StatusCode = StatusCodes.Status401Unauthorized, Times.Once);
        }
        [Fact]
        public async Task ParseRequestInfoAndMoveToNext()
        {
            int i = 0,
                expI = 15;
            string expUserId = "user-id",
                route = "/some-resource/part2/",
                expRequesteeId = "123",
                expPath = $"{route}/__public/{expRequesteeId}",
                expMethod = "some-method";

            var ecr = new EntityConfigRecord
            {
                ControllerSettings = new ControllerSettings
                {
                    Route = route,
                },
                Type = typeof(string),
            };
            var entityConfigRecords = new[] { ecr };
            RequestDelegate reqDel = hc =>
            {
                i = expI;
                return Task.CompletedTask;
            };

            var logger = new Mock<ILogger<WorkContextMiddleware>>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, expUserId) }));
            var ctx = new Mock<HttpContext>();
            var request = new Mock<HttpRequest>();
            request.Setup(r => r.Path).Returns(new PathString(expPath));
            request.Setup(r => r.Method).Returns(expMethod);

            var response = new Mock<HttpResponse>();
            ctx.Setup(h => h.User).Returns(user);
            ctx.Setup(h => h.Request).Returns(request.Object);
            ctx.Setup(h => h.Response).Returns(response.Object);
            var wc = new WorkContext();

            var wcm = new WorkContextMiddleware(reqDel, logger.Object, entityConfigRecords);
            await wcm.InvokeAsync(ctx.Object, wc);
            i.ShouldBe(expI);
            wc.CurrentEntityConfigRecord.ShouldBe(ecr);
            wc.RequestInfo.Path.ShouldBe(expPath);
            wc.RequestInfo.Method.ShouldBe(expMethod);
            wc.RequestInfo.RequesteeId.ShouldBe(expRequesteeId);
        }
    }
}